using UnityEngine;

public class PdftronInit : MonoBehaviour
{
    [SerializeField] string apryseLicenseKey = "YOUR-APRYSE-LICENSE-KEY"; // вставь свой ключ

    void Awake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        var pdfnet = new AndroidJavaClass("com.pdftron.pdf.PDFNet");
        pdfnet.CallStatic("initialize", activity, apryseLicenseKey);
        // опц: pdfnet.CallStatic("setTempPath", activity.Call<AndroidJavaObject>("getCacheDir").Call<string>("getAbsolutePath"));
        Debug.Log("[Apryse] Initialized");
#endif
    }
}