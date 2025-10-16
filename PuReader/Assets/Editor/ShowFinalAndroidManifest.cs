#if UNITY_EDITOR && UNITY_ANDROID
using UnityEditor;
using UnityEditor.Android;
using System.IO;

public class ShowFinalAndroidManifest : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => int.MaxValue;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        try
        {
            string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifestPath))
            {
                UnityEngine.Debug.LogWarning("[ShowFinalAndroidManifest] AndroidManifest.xml не найден: " + manifestPath);
                return;
            }

            string manifestText = File.ReadAllText(manifestPath);
            string logPath = Path.Combine(UnityEngine.Application.dataPath, "../LastFinalManifest.xml");
            File.WriteAllText(logPath, manifestText);

            UnityEngine.Debug.Log($"✅ [ShowFinalAndroidManifest] Финальный AndroidManifest сохранён:\n{logPath}");
            UnityEngine.Debug.Log("------ ПЕРВЫЕ 40 СТРОК ------\n" +
                string.Join("\n", manifestText.Split('\n'), 0, System.Math.Min(40, manifestText.Split('\n').Length)) +
                "\n------------------------------");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("[ShowFinalAndroidManifest] Ошибка: " + e.Message);
        }
    }
}
#endif
