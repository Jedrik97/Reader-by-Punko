using UnityEngine;

public class PdfIntentBootstrap : MonoBehaviour
{
    public PdfViewer viewer; // перетащи ссылку на объект с PdfViewer в инспекторе

    void Awake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var bridgeClass = new AndroidJavaClass("com.example.pdfrenderer.IntentPdfBridge");
            string importedPath = bridgeClass.CallStatic<string>("importPdfFromIntent", activity);

            if (!string.IsNullOrEmpty(importedPath) && viewer != null)
            {
                // Скажем вьюеру использовать файл из cache вместо StreamingAssets
                viewer.sourcePdfPath = importedPath;
                viewer.copyFromStreamingAssets = false;
                Debug.Log("[PdfIntentBootstrap] Open via intent: " + importedPath);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[PdfIntentBootstrap] " + e.Message);
        }
#endif
    }
}