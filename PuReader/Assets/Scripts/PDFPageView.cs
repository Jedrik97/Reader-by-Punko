using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;

public class PDFPageView : MonoBehaviour
{
    public RawImage image;
    public AspectRatioFitter aspect;
    public LayoutElement layout;

    int pageIndex;
    Func<Task> renderRequest;
    Texture2D tex;

    public bool HasTexture => tex != null;

    public void Setup(int index, Func<Task> onRenderRequest)
    {
        pageIndex = index;
        renderRequest = onRenderRequest;
        image.texture = null;
    }

    public void SetPlaceholderAspect(float aspectRatio)
    {
        if (aspect != null)
        {
            aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspect.aspectRatio = aspectRatio <= 0f ? 0.77f : aspectRatio;
        }
    }

    public async Task EnsureRendered()
    {
        if (HasTexture || renderRequest == null) return;
        await renderRequest.Invoke();
    }

    public void ApplyTexture(byte[] rgba, int w, int h)
    {
        if (tex != null) Destroy(tex);
        tex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
        tex.LoadRawTextureData(rgba);
        tex.Apply(false, true); // makeNoLongerReadable = true
        image.texture = tex;

        if (aspect != null)
            aspect.aspectRatio = (float)w / h;
    }

    public void TryUnloadIfFar(RectTransform viewport, float unloadDistanceScreens = 2.5f)
    {
        if (!HasTexture) return;

        // вычисляем насколько далеко страница от видимой области по вертикали
        Rect vpRect = GetWorldRect(viewport);
        Rect pageRect = GetWorldRect((RectTransform)transform);

        float screenH = vpRect.height;
        bool farAbove = (vpRect.yMax - pageRect.yMin) < -unloadDistanceScreens * screenH;
        bool farBelow = (pageRect.yMax - vpRect.yMin) < -unloadDistanceScreens * screenH;

        if (farAbove || farBelow)
            Unload();
    }

    public void Unload()
    {
        image.texture = null;
        if (tex != null) Destroy(tex);
        tex = null;
    }

    static Rect GetWorldRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return new Rect(corners[0], corners[2] - corners[0]);
    }

    void OnDestroy() { if (tex != null) Destroy(tex); }
}
