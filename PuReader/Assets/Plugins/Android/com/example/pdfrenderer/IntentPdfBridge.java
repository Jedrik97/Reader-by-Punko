package com.example.pdfrenderer;

import android.app.Activity;
import android.content.Intent;
import android.net.Uri;
import android.os.ParcelFileDescriptor;
import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;

public class IntentPdfBridge {

    // Вернёт абсолютный путь к скопированному во временную папку PDF, либо null
    public static String importPdfFromIntent(Activity act) {
        if (act == null) return null;
        Intent intent = act.getIntent();
        if (intent == null) return null;

        Uri uri = intent.getData();
        if (uri == null) return null;

        // Проверим тип
        String type = intent.getType();
        if (type == null || !type.equalsIgnoreCase("application/pdf")) {
            // иногда файловые менеджеры не ставят type — всё равно попробуем по расширению
            String p = uri.toString();
            if (p == null || (!p.toLowerCase().endsWith(".pdf"))) {
                // не PDF
                return null;
            }
        }

        try {
            // Копируем в cacheDir с временным именем
            File out = new File(act.getCacheDir(), "incoming.pdf");
            InputStream in = act.getContentResolver().openInputStream(uri);
            if (in == null) return null;

            FileOutputStream fos = new FileOutputStream(out);
            byte[] buf = new byte[8192];
            int n;
            while ((n = in.read(buf)) > 0) {
                fos.write(buf, 0, n);
            }
            fos.flush();
            fos.close();
            in.close();

            // Снимем флаг, чтобы следующий запуск через лаунчер не пытался снова парсить этот же интент
            act.setIntent(new Intent(act, act.getClass()));

            return out.getAbsolutePath();
        } catch (Exception e) {
            e.printStackTrace();
            return null;
        }
    }
}
