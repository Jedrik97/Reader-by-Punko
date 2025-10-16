package com.example.pdfrenderer;

import android.graphics.Bitmap;
import android.graphics.pdf.PdfRenderer;
import android.os.Build;
import android.os.ParcelFileDescriptor;

import java.io.File;
import java.io.IOException;

public class PdfRendererBridge {
    private PdfRenderer renderer;
    private PdfRenderer.Page currentPage;

    public boolean open(String path) throws IOException {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.LOLLIPOP) return false;
        File file = new File(path);
        ParcelFileDescriptor pfd = ParcelFileDescriptor.open(file, ParcelFileDescriptor.MODE_READ_ONLY);
        renderer = new PdfRenderer(pfd);
        return true;
    }

    public int getPageCount() {
        return renderer != null ? renderer.getPageCount() : 0;
    }

    public void close() {
        try {
            if (currentPage != null) currentPage.close();
            if (renderer != null) renderer.close();
        } catch (Exception ignored) {}
        currentPage = null;
        renderer = null;
    }

    public byte[] renderPageToRGBA(int index, int targetWidth, int targetHeight, int renderMode) throws IOException {
        if (renderer == null) throw new IOException("Renderer not opened");
        if (currentPage != null) currentPage.close();

        currentPage = renderer.openPage(index);
        int w = targetWidth > 0 ? targetWidth : currentPage.getWidth();
        int h = targetHeight > 0 ? targetHeight : currentPage.getHeight();

        Bitmap bmp = Bitmap.createBitmap(w, h, Bitmap.Config.ARGB_8888);
        currentPage.render(bmp, null, null,
                renderMode == 1 ? PdfRenderer.Page.RENDER_MODE_FOR_PRINT : PdfRenderer.Page.RENDER_MODE_FOR_DISPLAY);

        int[] argb = new int[w * h];
        bmp.getPixels(argb, 0, w, 0, 0, w, h);
        byte[] rgba = new byte[w * h * 4];
        int p = 0;
        for (int c : argb) {
            int a = (c >> 24) & 0xFF;
            int r = (c >> 16) & 0xFF;
            int g = (c >> 8) & 0xFF;
            int b = (c) & 0xFF;
            rgba[p++] = (byte) r;
            rgba[p++] = (byte) g;
            rgba[p++] = (byte) b;
            rgba[p++] = (byte) a;
        }
        bmp.recycle();
        return rgba;
    }

    public int getPageWidth(int index) throws IOException {
        PdfRenderer.Page page = renderer.openPage(index);
        int w = page.getWidth();
        page.close();
        return w;
    }

    public int getPageHeight(int index) throws IOException {
        PdfRenderer.Page page = renderer.openPage(index);
        int h = page.getHeight();
        page.close();
        return h;
    }
}
