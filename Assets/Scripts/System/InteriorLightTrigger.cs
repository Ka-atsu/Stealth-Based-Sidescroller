using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class InteriorLightTrigger : MonoBehaviour
{
    public Light2D interiorLight;
    public Light2D outsideLight;

    [Header("Light Values")]
    public float interiorBright = 0.15f;
    public float interiorDark = 0.05f;

    public float outsideBright = 0.15f;
    public float outsideDark = 0.05f;

    [Header("Transition")]
    public float transitionTime = 0.4f;

    private Coroutine currentTransition;

    private void Start()
    {
        interiorLight.intensity = interiorDark;
        outsideLight.intensity = outsideBright;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerController2D>();
        if (player == null) return;

        StartTransition(interiorBright, outsideDark);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerController2D>();
        if (player == null) return;

        StartTransition(interiorDark, outsideBright);
    }

    void StartTransition(float interiorTarget, float outsideTarget)
    {
        if (currentTransition != null)
            StopCoroutine(currentTransition);

        currentTransition = StartCoroutine(FadeLights(interiorTarget, outsideTarget));
    }

    IEnumerator FadeLights(float interiorTarget, float outsideTarget)
    {
        float startInterior = interiorLight.intensity;
        float startOutside = outsideLight.intensity;

        float t = 0;

        while (t < transitionTime)
        {
            t += Time.deltaTime;
            float progress = t / transitionTime;

            interiorLight.intensity = Mathf.Lerp(startInterior, interiorTarget, progress);
            outsideLight.intensity = Mathf.Lerp(startOutside, outsideTarget, progress);

            yield return null;
        }

        interiorLight.intensity = interiorTarget;
        outsideLight.intensity = outsideTarget;
    }
}