using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class InteriorLightTrigger : MonoBehaviour
{
    [SerializeField] private Light2D interiorLight;
    [SerializeField] private Light2D outsideLight;

    [Header("Light Values")]
    [SerializeField] private float interiorBright = 0.15f;
    [SerializeField] private float interiorDark = 0.05f;
    [SerializeField] private float outsideBright = 0.15f;
    [SerializeField] private float outsideDark = 0.05f;

    [Header("Transition")]
    [SerializeField] private float transitionTime = 0.4f;

    private Coroutine currentTransition;

    private void Start()
    {
        if (interiorLight != null)
            interiorLight.intensity = interiorDark;

        if (outsideLight != null)
            outsideLight.intensity = outsideBright;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActiveAndEnabled) return;
        if (GameSceneManager.Instance != null && GameSceneManager.Instance.IsLoadingScene) return;

        var player = other.GetComponentInParent<PlayerController2D>();
        if (player == null) return;

        StartTransition(interiorBright, outsideDark);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!isActiveAndEnabled) return;
        if (GameSceneManager.Instance != null && GameSceneManager.Instance.IsLoadingScene) return;

        var player = other.GetComponentInParent<PlayerController2D>();
        if (player == null) return;

        StartTransition(interiorDark, outsideBright);
    }

    private void OnDisable()
    {
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
            currentTransition = null;
        }
    }

    private void StartTransition(float interiorTarget, float outsideTarget)
    {
        if (!isActiveAndEnabled) return;
        if (interiorLight == null || outsideLight == null) return;

        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
        }

        currentTransition = StartCoroutine(FadeLights(interiorTarget, outsideTarget));
    }

    private IEnumerator FadeLights(float interiorTarget, float outsideTarget)
    {
        if (interiorLight == null || outsideLight == null)
            yield break;

        float startInterior = interiorLight.intensity;
        float startOutside = outsideLight.intensity;
        float t = 0f;

        while (t < transitionTime)
        {
            if (!isActiveAndEnabled || interiorLight == null || outsideLight == null)
                yield break;

            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / transitionTime);

            interiorLight.intensity = Mathf.Lerp(startInterior, interiorTarget, progress);
            outsideLight.intensity = Mathf.Lerp(startOutside, outsideTarget, progress);

            yield return null;
        }

        interiorLight.intensity = interiorTarget;
        outsideLight.intensity = outsideTarget;
        currentTransition = null;
    }
}