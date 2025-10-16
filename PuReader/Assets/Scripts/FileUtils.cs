using UnityEngine;
using System.IO;
using System.Threading.Tasks;

public static class FileUtils
{
    public static async Task<byte[]> ReadContentUriAsync(string uriStr)
    {
        var uri = new AndroidJavaObject("android.net.Uri", uriStr);
        var resolver = new AndroidJavaClass("android.content.ContextWrapper")
            .CallStatic<AndroidJavaObject>("getApplicationContext")
            .Call<AndroidJavaObject>("getContentResolver");

        AndroidJavaObject inputStream = resolver.Call<AndroidJavaObject>("openInputStream", uri);
        using var stream = new AndroidJavaObject("java.io.BufferedInputStream", inputStream);

        // Простейшее чтение
        MemoryStream ms = new MemoryStream();
        byte[] buffer = new byte[8192];
        int read;
        while ((read = stream.Call<int>("read", buffer)) != -1)
            ms.Write(buffer, 0, read);

        return ms.ToArray();
    }
}