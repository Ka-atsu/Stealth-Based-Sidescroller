using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController2D))]
public class PlayerInputHandler : MonoBehaviour
{
    PlayerController2D controller;
    PlayerGrapple2D grapple;

    Vector2 mousePosition;

    void Awake()
    {
        controller = GetComponent<PlayerController2D>();
        grapple = GetComponent<PlayerGrapple2D>();
    }

    public void OnMove(InputValue value)
    {
        controller.SetMove(value.Get<Vector2>());
    }

    public void OnJump(InputValue value)
    {
        controller.SetJumpHeld(value.isPressed);
    }

    public void OnSprint(InputValue value)
    {
        if (value.isPressed) controller.SetRunHeld(true);
    }

    public void OnSprintRelease(InputValue value)
    {
        controller.SetRunHeld(false);
    }

    public void OnCrouch(InputValue value)
    {
        if (value.isPressed) controller.SetCrouch(true);
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
}