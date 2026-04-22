using UnityEngine;

public class BossHitVFXSpawner2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform attackPoint;

    [Header("VFX")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 0.2f);
    [SerializeField] private bool parentToWorld = true;

    [Header("Feel")]
    [SerializeField] private float effectScaleMultiplier = 1f;
    [SerializeField] private float effectLifetimeMultiplier = 1f;

    public void SpawnHitEffect(Vector3 targetPosition)
    {
        if (hitEffectPrefab == null)
            return;

        Vector3 origin = attackPoint != null ? attackPoint.position : transform.position;
        Vector3 spawnPosition = ((origin + targetPosition) * 0.5f) + (Vector3)spawnOffset;

        GameObject effectInstance;

        if (parentToWorld)
            effectInstance = Instantiate(hitEffectPrefab, spawnPosition, Quaternion.identity);
        else
            effectInstance = Instantiate(hitEffectPrefab, spawnPosition, Quaternion.identity, transform);

        HakiFrameVFX2D frameVfx = effectInstance.GetComponent<HakiFrameVFX2D>();
        if (frameVfx == null)
            frameVfx = effectInstance.GetComponentInChildren<HakiFrameVFX2D>();

        if (frameVfx != null)
        {
            frameVfx.SetRuntimeMultipliers(effectScaleMultiplier, effectLifetimeMultiplier);
            return;
        }

        HakiVFX2D haki = effectInstance.GetComponent<HakiVFX2D>();
        if (haki == null)
            haki = effectInstance.GetComponentInChildren<HakiVFX2D>();

        if (haki != null)
            haki.SetRuntimeMultipliers(effectScaleMultiplier, effectLifetimeMultiplier);
    }
}