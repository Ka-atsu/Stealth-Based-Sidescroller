using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController2D))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerInputHandler : MonoBehaviour
{
    PlayerController2D controller;
    PlayerGrappleHang2D grapple;
    PlayerInput playerInput;

    Vector2 mousePosition;
    Vector2 moveInput;

    bool sprintHeld;

    void Awake()
    {
        controller = GetComponent<PlayerController2D>();
        grapple = GetComponent<PlayerGrappleHang2D>();
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
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
        controller.SetMove(moveInput);

        UpdateRunState();
    }

    public void OnJump(InputValue value)
    {
        controller.SetJumpHeld(value.isPressed);
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
        if (!value.isPressed) return;

        if (grapple != null)
            grapple.TryGrapple(mousePosition);
    }

    void UpdateRunState()
    {
        bool isMoving = Mathf.Abs(moveInput.x) > 0.01f || Mathf.Abs(moveInput.y) > 0.01f;
        controller.SetRunHeld(sprintHeld && isMoving);
    }
}