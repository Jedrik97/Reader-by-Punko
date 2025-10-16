using UnityEngine;
using System.IO;
using System;
using System.Threading.Tasks;

public class CacheManager
{
    private string cacheDir;

    public CacheManager()
    {
        cacheDir = Path.Combine(Application.persistentDataPath, "converted");
        Directory.CreateDirectory(cacheDir);
    }

    public string GetCachedPath(string key) =>
        Path.Combine(cacheDir, key + ".pdf");

    public async Task SaveAsync(string key, byte[] data)
    {
        string path = GetCachedPath(key);
        await File.WriteAllBytesAsync(path, data);
        File.WriteAllText(path + ".meta", DateTime.UtcNow.ToString("o"));
    }

    public void CleanupOld(int days = 7)
    {
        foreach (var meta in Directory.GetFiles(cacheDir, "*.meta"))
        {
            var lastAccess = DateTime.Parse(File.ReadAllText(meta));
            if ((DateTime.UtcNow - lastAccess).TotalDays > days)
            {
                File.Delete(meta);
                File.Delete(meta.Replace(".meta", ""));
            }
        }
    }
}