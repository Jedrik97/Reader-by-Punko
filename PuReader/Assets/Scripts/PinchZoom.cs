using UnityEngine;
using UnityEngine.EventSystems;

public class PinchZoom : MonoBehaviour, IScrollHandler
{
    public RectTransform target; // ZoomRoot
    public float minScale = 0.8f;
    public float maxScale = 3.0f;
    public float wheelSensitivity = 0.0015f;
    public PDFViewer viewer; // чтобы сообщать про зум

    float scale = 1f;
    float lastTapTime;
    const float doubleTapWindow = 0.25f;

    void Awake()
    {
        if (!target) target = (RectTransform)transform;
    }

    void Update()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount == 2)
        {
            var t0 = Input.GetTouch(0);
            var t1 = Input.GetTouch(1);
            if (t1.phase == TouchPhase.Began) return;

            float prev = (t0.position - t0.deltaPosition - (t1.position - t1.deltaPosition)).magnitude;
            float curr = (t0.position - t1.position).magnitude;
            float delta = (curr - prev) / Screen.dpi; // нормируем чуть-чуть
            ApplyDelta(delta * 0.8f);
        }
        if (Input.touchCount == 1)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Ended)
            {
                if (Time.time - lastTapTime < doubleTapWindow)
                {
                    // двойной тап — ресет зума
                    SetScale(1f);
                }
                lastTapTime = Time.time;
            }
        }
#endif
        // Кнопка Back = выход (двойной тап — опционально)
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }

    public void OnScroll(PointerEventData eventData)
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        ApplyDelta(eventData.scrollDelta.y * wheelSensitivity);
#endif
    }

    void ApplyDelta(float delta)
    {
        SetScale(Mathf.Clamp(scale + delta, minScale, maxScale));
    }

    void SetScale(float s)
    {
        scale = s;
        target.localScale = Vector3.one * scale;
        if (viewer) viewer.OnZoomChanged(scale);
    }
}
