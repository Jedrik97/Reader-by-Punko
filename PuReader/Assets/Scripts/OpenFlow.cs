using UnityEngine;
using System.IO;
using System.Threading.Tasks;

public class OpenFlow : MonoBehaviour
{
    CacheManager cache = new CacheManager();

    public PDFViewer pdfViewer;
    public async Task HandleExternalUriAsync(string uri)
    {
        Debug.Log("Handle uri: " + uri);

        string ext = Path.GetExtension(uri).ToLowerInvariant();
        string key = uri.GetHashCode().ToString();

        cache.CleanupOld();

        if (ext == ".pdf")
        {
            await OpenPdfAsync(uri);
        }
        else if (ext == ".docx" || ext == ".xlsx")
        {
            string cached = cache.GetCachedPath(key);
            if (!File.Exists(cached))
            {
                byte[] fileData = await FileUtils.ReadContentUriAsync(uri);
                byte[] pdfData = await ConvertOfficeToPdfAsync(fileData, ext);
                await cache.SaveAsync(key, pdfData);
            }

            await OpenPdfAsync("file://" + cached);
        }
        else
        {
            ShowUnsupportedDialog();
        }
    }

    private async Task<byte[]> ConvertOfficeToPdfAsync(byte[] src, string ext)
{
#if UNITY_ANDROID && !UNITY_EDITOR
    // 1) сохраняем офисный файл во временный путь
    string inPath = System.IO.Path.Combine(Application.temporaryCachePath, "in" + ext);
    await System.IO.File.WriteAllBytesAsync(inPath, src);

    // 2) готовим выходной путь
    string outPath = System.IO.Path.Combine(Application.temporaryCachePath, "converted_tmp.pdf");
    if (System.IO.File.Exists(outPath)) System.IO.File.Delete(outPath);

    // 3) дергаем Java-бридж (можно file:// — достаточно)
    var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
    var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
    var bridge = new AndroidJavaClass("com.punko.reader.ApryseBridge");

    string res = bridge.CallStatic<string>("convertOfficeToPdf", activity, "file://" + inPath, outPath);
    if (!res.StartsWith("ok"))
        throw new System.Exception("Apryse convert failed: " + res);

    // 4) читаем PDF байты и возвращаем
    return await System.IO.File.ReadAllBytesAsync(outPath);
#else
    await System.Threading.Tasks.Task.Yield();
    throw new System.NotSupportedException("Conversion works only on Android device.");
#endif
}


    private async Task OpenPdfAsync(string pathOrUri)
    {
        byte[] bytes;

        if (pathOrUri.StartsWith("file://"))
            bytes = System.IO.File.ReadAllBytes(new System.Uri(pathOrUri).LocalPath);
        else if (pathOrUri.StartsWith("content://"))
            bytes = await FileUtils.ReadContentUriAsync(pathOrUri);
        else
            bytes = System.IO.File.ReadAllBytes(pathOrUri);

        // Важное: фабрика рендера — сюда подставишь свой SDK
        await pdfViewer.LoadFromBytes(bytes, data => new DummyPdfRenderer(data));
    }

    private void ShowUnsupportedDialog()
    {
        Debug.LogWarning("Формат не поддерживается");
    }
}