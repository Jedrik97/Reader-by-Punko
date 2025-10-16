using UnityEngine;

public class PdfRendererAndroid : System.IDisposable
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject bridge;
#endif
    public bool Open(string path)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var cls = new AndroidJavaClass("com.example.pdfrenderer.PdfRendererBridge"))
        {
            bridge = new AndroidJavaObject("com.example.pdfrenderer.PdfRendererBridge");
            return bridge.Call<bool>("open", path);
        }
#else
        Debug.LogWarning("PdfRenderer works only on Android device.");
        return false;
#endif
    }

    public int PageCount
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return bridge?.Call<int>("getPageCount") ?? 0;
#else
            return 0;
#endif
        }
    }

    public void Dispose()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        bridge?.Call("close");
        bridge = null;
#endif
    }

    public byte[] RenderPageRGBA(int index, int width, int height, bool forPrint = false)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return bridge.Call<byte[]>("renderPageToRGBA", index, width, height, forPrint ? 1 : 0);
#else
        return null;
#endif
    }

    public Vector2Int GetPageSize(int index)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        int w = bridge.Call<int>("getPageWidth", index);
        int h = bridge.Call<int>("getPageHeight", index);
        return new Vector2Int(w, h);
#else
        return new Vector2Int(0, 0);
#endif
    }
}