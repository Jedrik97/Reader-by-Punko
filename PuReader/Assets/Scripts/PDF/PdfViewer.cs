using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class PdfViewer : MonoBehaviour
{
    public enum FitMode { FitBest, FitWidth, FitHeight }

    [Header("UI")]
    public RawImage pageView;
    public Text pageLabel;
    public float minZoom = 1f;
    public float maxZoom = 5f;
    public float doubleTapZoom = 2.5f;

    [Header("PDF")]
    public string sourcePdfPath;
    public bool copyFromStreamingAssets = true;

    [Header("Fitting")]
    public FitMode fitMode = FitMode.FitBest;   // << добавлено
    public bool autoRefitOnResize = true;       // << добавлено
    [Range(0.05f, 0.5f)] public float refitDebounce = 0.15f; // << добавлено

    private PdfRendererAndroid pdf;
    private Texture2D tex;
    private int currentPage = 0;
    private int pageCount = 0;

    private RectTransform rt;
    private float zoom = 1f;
    private Vector2 pan = Vector2.zero;
    private float doubleTapTime = 0.25f;
    private float lastTapTime = -1f;

    // отслеживание размера экрана
    private Vector2Int lastScreenSize;          // << добавлено
    private float pendingRefitAt = -1f;         // << добавлено

    void Awake()
    {
        rt = pageView.rectTransform;
    }

    void Start()
    {
        string path = PreparePdfPath(sourcePdfPath);
        pdf = new PdfRendererAndroid();
        if (!pdf.Open(path))
        {
            Debug.LogError("Failed to open PDF: " + path);
            return;
        }
        pageCount = pdf.PageCount;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height); // << добавлено
        LoadPage(currentPage, true);
        UpdatePageLabel();
    }

    void Update()
    {
        HandleGestures();
        HandlePaging();

        // авто-подгонка при повороте/изменении размеров
        if (autoRefitOnResize)
        {
            var nowSize = new Vector2Int(Screen.width, Screen.height);
            if (nowSize != lastScreenSize)
            {
                lastScreenSize = nowSize;
                // запускаем «дебаунс» — перерендер через короткую паузу
                pendingRefitAt = Time.unscaledTime + refitDebounce;
            }
            if (pendingRefitAt > 0 && Time.unscaledTime >= pendingRefitAt)
            {
                pendingRefitAt = -1f;
                RefitToScreen(); // << ключевая функция
            }
        }
    }

    // можно вызывать и вручную, если надо
    public void RefitToScreen()                 // << добавлено
    {
        // перерендер текущей страницы и сброс трансформа
        LoadPage(currentPage, true);
        UpdatePageLabel();
    }

    void OnRectTransformDimensionsChange()      // << добавлено
    {
        // подстрахуемся: если Canvas изменил размеры (например, появление системной панели)
        if (!autoRefitOnResize) return;
        pendingRefitAt = Time.unscaledTime + refitDebounce;
    }

    string PreparePdfPath(string src)
    {
        if (!copyFromStreamingAssets) return src;

        string inPath = Path.Combine(Application.streamingAssetsPath, src);
        string outPath = Path.Combine(Application.persistentDataPath, src);

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!File.Exists(outPath))
        {
            var uwr = UnityEngine.Networking.UnityWebRequest.Get(inPath);
            uwr.SendWebRequest();
            while (!uwr.isDone) { }
            File.WriteAllBytes(outPath, uwr.downloadHandler.data);
        }
        return outPath;
#else
        return inPath;
#endif
    }

    void LoadPage(int index, bool fitScreen)
    {
        var pagePx = pdf.GetPageSize(index); // «нативные» px страницы

        // целевой размер рендеринга под текущий экран
        var target = ComputeTargetRenderSize(pagePx, fitMode); // << перерасчёт под экран

        var data = pdf.RenderPageRGBA(index, target.x, target.y);
        if (tex == null || tex.width != target.x || tex.height != target.y)
        {
            if (tex != null) Destroy(tex);
            tex = new Texture2D(target.x, target.y, TextureFormat.RGBA32, false, false);
            tex.wrapMode = TextureWrapMode.Clamp;
        }
        tex.LoadRawTextureData(data);
        tex.Apply(false, false);

        pageView.texture = tex;

        if (fitScreen)
        {
            zoom = 1f;
            pan = Vector2.zero;
            ApplyTransform();
        }
    }

    // рассчитываем целевой размер рендеринга под текущую ориентацию/экран
    Vector2Int ComputeTargetRenderSize(Vector2Int pagePx, FitMode mode) // << добавлено
    {
        // реальные «экраные» пиксели под Canvas-Overlay
        int sw = Mathf.Max(1, Screen.width);
        int sh = Mathf.Max(1, Screen.height);

        float pageAspect = (float)pagePx.x / Mathf.Max(1, pagePx.y);
        float screenAspect = (float)sw / sh;

        int tw, th;

        switch (mode)
        {
            case FitMode.FitWidth:
                tw = Mathf.Clamp(sw, 256, 4096);
                th = Mathf.Clamp(Mathf.RoundToInt(tw / pageAspect), 256, 4096);
                break;

            case FitMode.FitHeight:
                th = Mathf.Clamp(sh, 256, 4096);
                tw = Mathf.Clamp(Mathf.RoundToInt(th * pageAspect), 256, 4096);
                break;

            default: // FitBest — вписать целиком (по меньшей стороне)
                bool limitByWidth = screenAspect <= pageAspect;
                if (limitByWidth)
                {
                    tw = Mathf.Clamp(sw, 256, 4096);
                    th = Mathf.Clamp(Mathf.RoundToInt(tw / pageAspect), 256, 4096);
                }
                else
                {
                    th = Mathf.Clamp(sh, 256, 4096);
                    tw = Mathf.Clamp(Mathf.RoundToInt(th * pageAspect), 256, 4096);
                }
                break;
        }

        // можно слегка «перерисовать с запасом» для зума ×1.25, если хочешь супер-чёткости:
        float oversample = 1.0f; // поставь 1.25f при желании
        tw = Mathf.Clamp(Mathf.RoundToInt(tw * oversample), 256, 4096);
        th = Mathf.Clamp(Mathf.RoundToInt(th * oversample), 256, 4096);

        return new Vector2Int(tw, th);
    }

    void HandlePaging()
    {
        if (Input.touchCount == 1)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Ended)
            {
                float dx = t.position.x - t.rawPosition.x;
                float thresh = Screen.width * 0.12f;
                if (dx > thresh) PrevPage();
                else if (dx < -thresh) NextPage();
            }
        }
        if (Input.GetKeyDown(KeyCode.RightArrow)) NextPage();
        if (Input.GetKeyDown(KeyCode.LeftArrow)) PrevPage();
    }

    void HandleGestures()
    {
        if (Input.touchCount == 2)
        {
            var t0 = Input.GetTouch(0);
            var t1 = Input.GetTouch(1);
            if (t1.phase == TouchPhase.Began) return;

            float prevDist = (t0.position - t0.deltaPosition - (t1.position - t1.deltaPosition)).magnitude;
            float currDist = (t0.position - t1.position).magnitude;
            float delta = (currDist - prevDist) / Screen.dpi;

            float prevZoom = zoom;
            zoom = Mathf.Clamp(zoom + delta, minZoom, maxZoom);

            Vector2 mid = (t0.position + t1.position) * 0.5f;
            Vector2 canvasSpace = ScreenToCanvas(mid);
            pan += (canvasSpace * (prevZoom - zoom));
            ApplyTransform();
        }
        else if (Input.touchCount == 1)
        {
            var t = Input.GetTouch(0);
            if (t.tapCount == 1 && t.phase == TouchPhase.Ended)
            {
                if (Time.time - lastTapTime < doubleTapTime)
                {
                    zoom = (zoom < (doubleTapZoom - 0.01f)) ? doubleTapZoom : 1f;
                    pan = Vector2.zero;
                    ApplyTransform();
                    lastTapTime = -1f;
                }
                else
                {
                    lastTapTime = Time.time;
                }
            }

            if (t.phase == TouchPhase.Moved && zoom > 1f)
            {
                Vector2 delta = t.deltaPosition;
                pan += delta / (Screen.dpi * 0.1f);
                ApplyTransform();
            }
        }
    }

    Vector2 ScreenToCanvas(Vector2 screenPos)
    {
        return (screenPos - new Vector2(Screen.width, Screen.height) * 0.5f) / (Screen.dpi * 0.1f);
    }

    void ApplyTransform()
    {
        rt.localScale = new Vector3(zoom, zoom, 1);
        rt.anchoredPosition = pan;
    }

    public void NextPage()
    {
        if (currentPage < pageCount - 1)
        {
            currentPage++;
            LoadPage(currentPage, false);
            UpdatePageLabel();
        }
    }

    public void PrevPage()
    {
        if (currentPage > 0)
        {
            currentPage++;
            currentPage -= 2;
            currentPage = Mathf.Max(0, currentPage);
            LoadPage(currentPage, false);
            UpdatePageLabel();
        }
    }

    void UpdatePageLabel()
    {
        if (pageLabel) pageLabel.text = $"{currentPage + 1} / {pageCount}";
    }

    void OnDestroy()
    {
        if (tex != null) Destroy(tex);
        pdf?.Dispose();
    }
}
