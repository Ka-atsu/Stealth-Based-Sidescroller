using UnityEngine;

public class PlayerAnimationEventRelay : MonoBehaviour
{
    private PlayerJump2D jump;
    private PlayerStealthStrike2D stealthStrike;
    private PlayerHealth playerHealth;

    void Awake()
    {
        jump = GetComponentInParent<PlayerJump2D>();
        stealthStrike = GetComponentInParent<PlayerStealthStrike2D>();
        playerHealth = GetComponentInParent<PlayerHealth>();
    }

    public void ReleaseGroundJumpFromAnimation()
    {
        jump?.ReleaseGroundJumpFromAnimation();
    }

    public void OnStealthStrikeHit()
    {
        stealthStrike?.OnStealthStrikeHit();
    }

    public void OnStealthStrikeFinished()
    {
        stealthStrike?.OnStealthStrikeFinished();
    }

    public void OnDeathAnimationFinished()
    {
        playerHealth?.OnDeathAnimationFinished();
    }
}