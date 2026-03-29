using System;
using UnityEngine;

[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerCrouch2D : MonoBehaviour
{
    [Header("Crouch Collider")]
    public Vector2 crouchColliderSize = new Vector2(0.8f, 1f);
    public Vector2 crouchColliderOffset = new Vector2(0f, -0.5f);

    [Header("Stand Check")]
    [SerializeField] LayerMask obstructionMask;
    [SerializeField] float standCheckSkin = 0.02f;

    [Header("Visual Juice")]
    [SerializeField] Transform visuals;
    [SerializeField] float crouchVisualHeight = 0.82f;
    [SerializeField] float crouchVisualWidth = 1.06f;
    [SerializeField] float crouchVisualOffsetY = -0.08f;
    [SerializeField] float visualLerpSpeed = 16f;

    public bool IsCrouching { get; private set; }
    public bool IsBlockedStanding => IsStandBlocked();

    public Action OnCrouchStarted;
    public Action OnCrouchEnded;
    public Action OnStandBlocked;

    CapsuleCollider2D capsule;

    Vector2 originalSize;
    Vector2 originalOffset;

    Vector3 visualsBaseLocalScale;
    Vector3 visualsBaseLocalPosition;
    bool visualsIsRoot;

    void Awake()
    {
        capsule = GetComponent<CapsuleCollider2D>();

        originalSize = capsule.size;
        originalOffset = capsule.offset;

        ResolveVisualsReference();

        if (visuals != null)
        {
            visualsBaseLocalScale = visuals.localScale;
            visualsBaseLocalPosition = visuals.localPosition;
        }
    }

    void Update()
    {
        UpdateVisuals(Time.deltaTime);
    }

    public void SetCrouch(bool crouch)
    {
        if (crouch)
        {
            if (IsCrouching) return;

            IsCrouching = true;
            ApplyCrouch();
            OnCrouchStarted?.Invoke();
            return;
        }

        if (!IsCrouching) return;

        if (IsStandBlocked())
        {
            OnStandBlocked?.Invoke();
            return;
        }

        IsCrouching = false;
        ApplyStand();
        OnCrouchEnded?.Invoke();
    }

    void ApplyCrouch()
    {
        float heightDiff = originalSize.y - crouchColliderSize.y;

        capsule.size = crouchColliderSize;
        capsule.offset = new Vector2(
            originalOffset.x,
            originalOffset.y - heightDiff * 0.5f
        );
    }

    void ApplyStand()
    {
        capsule.size = originalSize;
        capsule.offset = originalOffset;
    }

    bool IsStandBlocked()
    {
        if (obstructionMask.value == 0) return false;

        Vector2 worldSize = GetWorldSize(originalSize - Vector2.one * standCheckSkin);
        Vector2 worldCenter = (Vector2)transform.position + originalOffset;

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            worldCenter,
            worldSize,
            0f,
            obstructionMask
        );

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;
            if (hits[i] == capsule) continue;
            if (hits[i].transform == transform) continue;
            return true;
        }

        return false;
    }

    Vector2 GetWorldSize(Vector2 localSize)
    {
        Vector3 lossy = transform.lossyScale;

        return new Vector2(
            Mathf.Abs(localSize.x * lossy.x),
            Mathf.Abs(localSize.y * lossy.y)
        );
    }

    void ResolveVisualsReference()
    {
        if (visuals == null)
        {
            Transform found = transform.Find("Visuals");
            visuals = found != null ? found : transform;
        }

        visualsIsRoot = visuals == transform;
    }

    void UpdateVisuals(float dt)
    {
        if (visuals == null) return;
        if (visualsIsRoot) return;

        Vector3 targetScale = IsCrouching
            ? new Vector3(
                visualsBaseLocalScale.x * crouchVisualWidth,
                visualsBaseLocalScale.y * crouchVisualHeight,
                visualsBaseLocalScale.z
            )
            : visualsBaseLocalScale;

        Vector3 targetPosition = IsCrouching
            ? visualsBaseLocalPosition + new Vector3(0f, crouchVisualOffsetY, 0f)
            : visualsBaseLocalPosition;

        visuals.localScale = Vector3.Lerp(visuals.localScale, targetScale, visualLerpSpeed * dt);
        visuals.localPosition = Vector3.Lerp(visuals.localPosition, targetPosition, visualLerpSpeed * dt);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        if (capsule == null) return;

        Gizmos.color = IsStandBlocked() ? Color.red : Color.green;

        Vector2 size = GetWorldSize(originalSize - Vector2.one * standCheckSkin);
        Vector2 center = (Vector2)transform.position + originalOffset;

        Gizmos.DrawWireCube(center, size);
    }
#endif
}