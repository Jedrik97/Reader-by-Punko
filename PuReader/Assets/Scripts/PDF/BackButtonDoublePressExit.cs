using UnityEngine;

public class BackButtonDoublePressExit : MonoBehaviour
{
    public float interval = 1.5f;
    private float lastBackTime = -10f;

#if UNITY_ANDROID && !UNITY_EDITOR
    AndroidJavaObject unityActivity;
    AndroidJavaClass toastClass;
#endif

    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        toastClass = new AndroidJavaClass("android.widget.Toast");
#endif
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            float t = Time.time;
            if (t - lastBackTime < interval)
            {
                Application.Quit();
            }
            else
            {
                lastBackTime = t;
                ShowToast("Нажмите ещё раз, чтобы выйти");
            }
        }
    }

    void ShowToast(string msg)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
        {
            AndroidJavaObject toast = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity, msg, 0);
            toast.Call("show");
        }));
#endif
    }
}