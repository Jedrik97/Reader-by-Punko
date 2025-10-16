using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Вертикальный просмотрщик PDF со скроллом, пинч-зумом (через PinchZoom на ZoomRoot),
/// ленивой подгрузкой видимых страниц и выгрузкой далёких текстур.
/// Работает с любым рендером через IPdfRenderer (AprysePdfRenderer / DummyPdfRenderer).
/// </summary>
public class PDFViewer : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Главный ScrollRect для вертикальной прокрутки")]
    public ScrollRect scrollRect;

    [Tooltip("Видимая область (с RectMask2D)")]
    public RectTransform viewport;

    [Tooltip("Контейнер, который масштабируется зумом")]
    public RectTransform zoomRoot;

    [Tooltip("Контент-колонка с VerticalLayoutGroup + ContentSizeFitter")]
    public RectTransform content;

    [Tooltip("Префаб страницы (RawImage + PDFPageView + AspectRatioFitter + LayoutElement)")]
    public PDFPageView pagePrefab;

    [Header("Render/DPI")]
    [Range(72, 300)]
    [Tooltip("Базовый DPI рендера видимых страниц (при scale=1)")]
    public float baseDpi = 150f;

    [Tooltip("Сколько страниц дополнительно рендерить вокруг центральной видимой")]
    public int preload = 2;

    [Tooltip("Во сколько высот экрана страница должна уйти, чтобы её выгрузить из памяти")]
    public float unloadDistance = 2.5f;

    // ===== runtime =====
    private IPdfRenderer renderer;
    private readonly List<PDFPageView> pages = new List<PDFPageView>();
    private bool initialized;
    private float currentScale = 1f;
    private int lastCenterIndex = -1;
    private float lastUpdateTime;
    private const float updateInterval = 0.05f; // троттлинг обновлений (20 Гц)

    /// <summary> Загрузка PDF из байтов и подготовка страниц. </summary>
    /// <param name="pdfBytes">PDF-файл в памяти</param>
    /// <param name="rendererFactory">Фабрика рендера: bytes -> IPdfRenderer</param>
    public async Task LoadFromBytes(byte[] pdfBytes, System.Func<byte[], IPdfRenderer> rendererFactory)
    {
        Clear();

        renderer = rendererFactory != null
            ? rendererFactory(pdfBytes)
            : null;

        if (renderer == null)
        {
            Debug.LogError("[PDFViewer] Renderer factory returned null.");
            return;
        }

        int count = renderer.PageCount;
        if (count <= 0)
        {
            Debug.LogError("[PDFViewer] No pages in PDF.");
            renderer.Dispose(); renderer = null;
            return;
        }

        // Создаём пустые ячейки под страницы
        for (int i = 0; i < count; i++)
        {
            var view = Instantiate(pagePrefab, content);
            view.name = $"Page_{i + 1}";
            int pageIndex = i; // замыкание
            view.Setup(pageIndex, () => RequestRender(pageIndex));
            pages.Add(view);

            // Выставим примерное соотношение сторон до реального рендера
            var (w, h) = renderer.GetPageSizePx(pageIndex, baseDpi);
            float aspect = (w > 0 && h > 0) ? (float)w / h : 0.707f; // ~A4
            view.SetPlaceholderAspect(aspect);
        }

        // Стабилизируем лайаут, чтобы ScrollRect знал высоту
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        initialized = true;
        lastCenterIndex = -1;
        await UpdateVisibilityAndLoads(true);
    }

    /// <summary> Запросить рендер одной страницы в нужном размере. </summary>
    private async Task RequestRender(int index)
    {
        if (renderer == null || index < 0 || index >= pages.Count) return;

        var (w0, h0) = renderer.GetPageSizePx(index, baseDpi);
        if (w0 <= 0 || h0 <= 0) return;

        // Масштаб по текущему зуму (ограничим ×3 от базового DPI)
        float dpi = Mathf.Clamp(baseDpi * currentScale, baseDpi, baseDpi * 3f);
        float scale = dpi / baseDpi;
        int w = Mathf.CeilToInt(w0 * scale);
        int h = Mathf.CeilToInt(h0 * scale);

        // Рендерим в фоновой таске
        byte[] rgba = await Task.Run(() => renderer.RenderPageRGBA(index, w, h, dpi));
        if (rgba == null || rgba.Length == 0) return;

        // Применяем в UI-потоке
        pages[index].ApplyTexture(rgba, w, h);
    }

    private void Update()
    {
        if (!initialized) return;

        // Троттлинг, чтобы не спамить расчётом на каждом кадре
        if (Time.unscaledTime - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.unscaledTime;

        _ = UpdateVisibilityAndLoads(false);
    }

    /// <summary> Обновляет центральную страницу, догружает соседние и выгружает далёкие. </summary>
    private async Task UpdateVisibilityAndLoads(bool forceImmediate)
    {
        if (!initialized || pages.Count == 0) return;

        int center = FindNearestVisiblePageIndex();
        if (center < 0) return;

        // Подгрузка окрестности
        int left = Mathf.Max(0, center - preload);
        int right = Mathf.Min(pages.Count - 1, center + preload);

        // Чтобы при быстром скролле не рендерить всё подряд — грузим сначала центр, потом расходясь
        if (forceImmediate || center != lastCenterIndex)
        {
            // центр
            if (!pages[center].HasTexture) await pages[center].EnsureRendered();

            // кольцом вправо/влево
            for (int radius = 1; radius <= preload; radius++)
            {
                int li = center - radius;
                int ri = center + radius;
                if (li >= left && li >= 0 && !pages[li].HasTexture) await pages[li].EnsureRendered();
                if (ri <= right && ri < pages.Count && !pages[ri].HasTexture) await pages[ri].EnsureRendered();
            }
            lastCenterIndex = center;
        }

        // Выгрузка далеко ушедших
        for (int i = 0; i < pages.Count; i++)
        {
            if (Mathf.Abs(i - center) > preload + 2)
                pages[i].TryUnloadIfFar(viewport, unloadDistance);
        }
    }

    /// <summary> Ищет страницу, центр которой ближе всего к центру viewport (по Y). </summary>
    private int FindNearestVisiblePageIndex()
    {
        float bestDist = float.MaxValue;
        int best = -1;

        Vector3 vpCenter = viewport.TransformPoint(viewport.rect.center);
        for (int i = 0; i < pages.Count; i++)
        {
            var r = (RectTransform)pages[i].transform;
            Vector3 pageCenter = r.TransformPoint(r.rect.center);
            float d = Mathf.Abs(pageCenter.y - vpCenter.y);
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    /// <summary> Сообщение от PinchZoom — зум изменился. </summary>
    public void OnZoomChanged(float scale)
    {
        currentScale = Mathf.Max(0.01f, scale);
        // После заметного зума можно перерендерить текущую окрестность плотнее.
        _ = UpdateVisibilityAndLoads(true);
    }

    /// <summary> Полная очистка (при загрузке нового PDF). </summary>
    public void Clear()
    {
        initialized = false;
        lastCenterIndex = -1;

        // удаляем элементы UI
        for (int i = 0; i < pages.Count; i++)
        {
            if (pages[i]) Destroy(pages[i].gameObject);
        }
        pages.Clear();

        // освобождаем движок/ресурсы
        renderer?.Dispose();
        renderer = null;

        // GC + выгрузка неиспользуемых ресурсов
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }
}
