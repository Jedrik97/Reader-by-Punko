using UnityEngine;
public class DummyPdfRenderer : IPdfRenderer
{
    int pages;
    public DummyPdfRenderer(byte[] data) { pages = 8; } // фиктивно 8 страниц
    public int PageCount => pages;
    public (int width, int height) GetPageSizePx(int pageIndex, float dpi = 150f) => (850, 1100);
    public byte[] RenderPageRGBA(int pageIndex, int w, int h, float dpi = 150f)
    {
        var bytes = new byte[w * h * 4];
        // закрашиваем серым + номер страницы «полосой»
        for (int i = 0; i < bytes.Length; i += 4) { bytes[i] = 230; bytes[i+1]=230; bytes[i+2]=230; bytes[i+3]=255; }
        for (int y = 0; y < 80; y++)
        for (int x = 0; x < w; x++)
        {
            int idx = (y * w + x) * 4;
            bytes[idx] = 180; bytes[idx+1]=180; bytes[idx+2]=180;
        }
        return bytes;
    }
    public void Dispose() {}
}