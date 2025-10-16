public interface IPdfRenderer : System.IDisposable
{
    int PageCount { get; }
    // Вернуть размер страницы в пунктах (pt) или пикселях — выбираем пиксели при базовом DPI
    (int width, int height) GetPageSizePx(int pageIndex, float dpi = 150f);
    // Синхронный или асинхронный рендер в Texture2D-compatible byte[]
    byte[] RenderPageRGBA(int pageIndex, int targetWidth, int targetHeight, float dpi = 150f);
}
