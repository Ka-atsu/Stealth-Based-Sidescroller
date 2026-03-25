using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HitFlash : MonoBehaviour
{
    public static HitFlash Instance;

    [SerializeField] private Image flashImage;
    [SerializeField] private float flashAlpha = 0.8f;
    [SerializeField] private float fadeSpeed = 10f;

    private Coroutine flashRoutine;

    private void Awake()
    {
        Instance = this;

        if (flashImage != null)
        {
            Color c = flashImage.color;
            c.a = 0f;
            flashImage.color = c;
        }
    }

    public void Flash()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // instant white flash
        Color c = flashImage.color;
        c.a = flashAlpha;
        flashImage.color = c;

        yield return null; // 👈 1 FRAME

        // fade out
        while (flashImage.color.a > 0f)
        {
            c.a -= fadeSpeed * Time.unscaledDeltaTime;
            flashImage.color = c;
            yield return null;
        }

        c.a = 0f;
        flashImage.color = c;
        flashRoutine = null;
    }
}