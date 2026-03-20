using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController2D))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerInputHandler : MonoBehaviour
{
    PlayerController2D controller;
    PlayerGrappleHang2D grapple;
    PlayerSwing2D swing;
    PlayerInput playerInput;

    Vector2 mousePosition;
    Vector2 moveInput;

    bool sprintHeld;
    IInteractable currentInteractable;

    void Awake()
    {
        controller = GetComponent<PlayerController2D>();
        grapple = GetComponent<PlayerGrappleHang2D>();
        swing = GetComponent<PlayerSwing2D>();
        playerInput = GetComponent<PlayerInput>();
    }

    public void EnableControls()
    {
        playerInput.enabled = true;
    }

    public void DisableControls()
    {
        playerInput.enabled = false;

        controller.SetMove(Vector2.zero);
        controller.SetRunHeld(false);
        controller.SetJumpHeld(false);

        moveInput = Vector2.zero;
        sprintHeld = false;

        if (swing != null)
            swing.SetMoveInput(Vector2.zero);
    }

    public void SetCurrentInteractable(IInteractable interactable)
    {
        currentInteractable = interactable;
    }

    public void ClearCurrentInteractable(IInteractable interactable)
    {
        if (currentInteractable == interactable)
            currentInteractable = null;
    }

    public void OnInteract(InputValue value)
    {
        Debug.Log("Interact pressed");

        if (!value.isPressed) return;
        currentInteractable?.Interact();
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
        controller.SetMove(moveInput);

        if (swing != null)
            swing.SetMoveInput(moveInput);

        UpdateRunState();
    }

    public void OnJump(InputValue value)
    {
        controller.SetJumpHeld(value.isPressed);

        if (value.isPressed && swing != null && swing.IsSwinging)
            swing.ReleaseSwing();
    }

    public void OnSprint(InputValue value)
    {
        sprintHeld = value.isPressed;
        UpdateRunState();
    }

    public void OnSprintRelease(InputValue value)
    {
        sprintHeld = false;
        UpdateRunState();
    }

    public void OnCrouch(InputValue value)
    {
        if (value.isPressed)
            controller.SetCrouch(true);
    }

    public void OnCrouchRelease(InputValue value)
    {
        controller.SetCrouch(false);
    }

    public void OnDash(InputValue value)
    {
        if (!value.isPressed) return;
        controller.TryDash();
    }

    public void OnAttack(InputValue value)
    {
        if (!value.isPressed) return;
        controller.TryStealthStrike();
    }

    public void OnLook(InputValue value)
    {
        mousePosition = value.Get<Vector2>();
    }

    public void OnGrapple(InputValue value)
    {
        if (swing != null)
        {
            if (value.isPressed)
                swing.TryGrapple(mousePosition);
            else
                swing.ReleaseSwing();

            return;
        }

        if (grapple != null && value.isPressed)
            grapple.TryGrapple(mousePosition);
    }

    void UpdateRunState()
    {
        bool isMovingHorizontally = Mathf.Abs(moveInput.x) > 0.01f;
        controller.SetRunHeld(sprintHeld && isMovingHorizontally);
    }
}