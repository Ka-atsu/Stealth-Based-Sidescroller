using UnityEngine;

public class FakeLanternBeam2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform beamVisual;
    [SerializeField] private SpriteRenderer beamRenderer;

    [Header("Raycast")]
    [SerializeField] private LayerMask blockerLayers;
    [SerializeField] private float maxDistance = 4f;
    [SerializeField] private Vector2 localBeamDirection = Vector2.right;

    [Header("Beam Size")]
    [SerializeField] private float baseBeamLength = 4f;
    [SerializeField] private float minBeamLength = 0.15f;
    [SerializeField] private float beamThickness = 1f;

    [Header("Offset")]
    [SerializeField] private float startOffset = 0.05f;

    private Vector3 initialScale;

    private void Awake()
    {
        if (beamVisual != null)
            initialScale = beamVisual.localScale;
    }

    private void LateUpdate()
    {
        UpdateBeam();
    }

    private void UpdateBeam()
    {
        if (beamVisual == null)
            return;

        Vector2 origin = transform.position + (Vector3)((Vector2)(transform.right * startOffset));
        Vector2 worldDirection = transform.TransformDirection(localBeamDirection.normalized);

        RaycastHit2D hit = Physics2D.Raycast(origin, worldDirection, maxDistance, blockerLayers);

        float targetLength = baseBeamLength;

        if (hit.collider != null)
            targetLength = Mathf.Max(minBeamLength, hit.distance);

        Vector3 scale = initialScale;
        scale.x = targetLength;
        scale.y = beamThickness;
        beamVisual.localScale = scale;

        if (beamRenderer != null)
            beamRenderer.enabled = targetLength > 0.01f;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector2 origin = transform.position + (Vector3)((Vector2)(transform.right * startOffset));
        Vector2 worldDirection = transform.TransformDirection(localBeamDirection.normalized);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + worldDirection * maxDistance);
    }
#endif
}