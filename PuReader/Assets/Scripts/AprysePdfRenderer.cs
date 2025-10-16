#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;

public class AprysePdfRenderer : IPdfRenderer
{
    private readonly string pdfPath;
    private readonly AndroidJavaClass bridge;

    public AprysePdfRenderer(byte[] pdfBytes)
    {
        pdfPath = System.IO.Path.Combine(Application.temporaryCachePath, "viewer_current.pdf");
        System.IO.File.WriteAllBytes(pdfPath, pdfBytes);
        bridge = new AndroidJavaClass("com.punko.reader.ApryseBridge");
    }

    public int PageCount => bridge.CallStatic<int>("getPageCount", pdfPath);

    public (int width, int height) GetPageSizePx(int pageIndex, float dpi = 150f)
    {
        int[] arr = bridge.CallStatic<int[]>("getPageSizePx", pdfPath, pageIndex, dpi);
        return (arr[0], arr[1]);
    }

    public byte[] RenderPageRGBA(int pageIndex, int targetWidth, int targetHeight, float dpi = 150f)
    {
        // dpi не используем, т.к. рендерим ровно в нужный размер текстуры
        return bridge.CallStatic<byte[]>("renderPageRGBA", pdfPath, pageIndex, targetWidth, targetHeight);
    }

    public void Dispose()
    {
        try { if (System.IO.File.Exists(pdfPath)) System.IO.File.Delete(pdfPath); } catch {}
    }
}
#endif