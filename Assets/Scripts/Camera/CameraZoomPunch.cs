using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class CameraZoomPunch : MonoBehaviour
{
    public static CameraZoomPunch Instance;

    private Camera cam;
    private float defaultSize;
    private Coroutine zoomRoutine;

    private void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        defaultSize = cam.orthographicSize;
    }

    public void Punch(float amount, float duration)
    {
        if (zoomRoutine != null)
            StopCoroutine(zoomRoutine);

        zoomRoutine = StartCoroutine(PunchRoutine(amount, duration));
    }

    private IEnumerator PunchRoutine(float amount, float duration)
    {
        float target = defaultSize - amount;

        cam.orthographicSize = target;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cam.orthographicSize = Mathf.Lerp(target, defaultSize, t / duration);
            yield return null;
        }

        cam.orthographicSize = defaultSize;
    }
}