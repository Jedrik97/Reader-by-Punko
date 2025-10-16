package com.punko.reader;

import android.content.Context;
import android.net.Uri;
import android.graphics.Bitmap;

import com.pdftron.pdf.PDFDoc;
import com.pdftron.pdf.Page;
import com.pdftron.pdf.PDFDraw;
import com.pdftron.pdf.Convert;
import com.pdftron.pdf.OfficeToPDFOptions;
import com.pdftron.sdf.SDFDoc;

public class ApryseBridge {

    public static String convertOfficeToPdf(Context ctx, String inputUriStr, String outPdfPath) {
        PDFDoc outDoc = null;
        try {
            Uri inputUri = Uri.parse(inputUriStr);
            outDoc = new PDFDoc();
            OfficeToPDFOptions opts = new OfficeToPDFOptions();
            Convert.officeToPdf(ctx, outDoc, inputUri, opts);
            outDoc.save(outPdfPath, SDFDoc.SaveMode.REMOVE_UNUSED, null);
            return "ok";
        } catch (Throwable t) {
            return "err: " + t.getMessage();
        } finally {
            try { if (outDoc != null) outDoc.close(); } catch (Exception ignore) {}
        }
    }

    public static int getPageCount(String pdfPath) {
        PDFDoc doc = null;
        try {
            doc = new PDFDoc(pdfPath);
            return doc.getPageCount();
        } catch (Throwable t) {
            return -1;
        } finally {
            try { if (doc != null) doc.close(); } catch (Exception ignore) {}
        }
    }

    public static int[] getPageSizePx(String pdfPath, int pageIndex0, float dpi) {
        PDFDoc doc = null;
        try {
            doc = new PDFDoc(pdfPath);
            Page p = doc.getPage(pageIndex0 + 1);
            double wPt = p.getPageWidth();
            double hPt = p.getPageHeight();
            int wPx = (int)Math.ceil(wPt * dpi / 72.0);
            int hPx = (int)Math.ceil(hPt * dpi / 72.0);
            return new int[]{ wPx, hPx };
        } catch (Throwable t) {
            return new int[]{ 0, 0 };
        } finally {
            try { if (doc != null) doc.close(); } catch (Exception ignore) {}
        }
    }

    public static byte[] renderPageRGBA(String pdfPath, int pageIndex0, int targetW, int targetH) {
        PDFDoc doc = null;
        try {
            doc = new PDFDoc(pdfPath);
            Page page = doc.getPage(pageIndex0 + 1);

            PDFDraw draw = new PDFDraw();
            draw.setImageSize(targetW, targetH, true, true);

            Bitmap bmp = Bitmap.createBitmap(targetW, targetH, Bitmap.Config.ARGB_8888);
            draw.export(page, bmp);

            int[] colors = new int[targetW * targetH];
            bmp.getPixels(colors, 0, targetW, 0, 0, targetW, targetH);

            byte[] out = new byte[targetW * targetH * 4];
            int o = 0;
            for (int c : colors) {
                int a = (c >>> 24) & 0xFF;
                int r = (c >>> 16) & 0xFF;
                int g = (c >>> 8) & 0xFF;
                int b = (c) & 0xFF;
                out[o++] = (byte) r;
                out[o++] = (byte) g;
                out[o++] = (byte) b;
                out[o++] = (byte) a;
            }
            bmp.recycle();
            return out;
        } catch (Throwable t) {
            return null;
        } finally {
            try { if (doc != null) doc.close(); } catch (Exception ignore) {}
        }
    }
}
