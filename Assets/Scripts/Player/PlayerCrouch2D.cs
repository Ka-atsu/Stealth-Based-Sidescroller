using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PlayerCrouch2D : MonoBehaviour
{
    [Header("Crouch")]
    public Vector2 crouchColliderSize = new Vector2(1f, 1f);
    public Vector2 crouchColliderOffset = new Vector2(0f, -0.5f);

    public bool IsCrouching { get; private set; }

    BoxCollider2D box;
    Vector2 originalSize;
    Vector2 originalOffset;

    void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        originalSize = box.size;
        originalOffset = box.offset;
    }

    public void SetCrouch(bool crouch)
    {
        if (IsCrouching == crouch) return;

        IsCrouching = crouch;

        if (crouch) ApplyCrouch();
        else ApplyStand();
    }

    void ApplyCrouch()
    {
        float heightDiff = originalSize.y - crouchColliderSize.y;

        box.size = crouchColliderSize;
        box.offset = new Vector2(
            originalOffset.x,
            originalOffset.y - heightDiff / 2f
        );
    }

    void ApplyStand()
    {
        box.size = originalSize;
        box.offset = originalOffset;
    }
}