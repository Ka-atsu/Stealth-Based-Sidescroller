using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttackSlashSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject slashEffectPrefab;
    [SerializeField] private Transform slashSpawnPoint;
    [SerializeField] private PlayerController2D playerController;

    [Header("Input")]
    [SerializeField] private InputActionReference attackAction;

    [Header("Fallback Offset")]
    [SerializeField] private Vector3 slashOffset = new Vector3(1f, 0.4f, 0f);

    private void OnEnable()
    {
        if (attackAction != null && attackAction.action != null)
        {
            attackAction.action.performed += OnAttackPerformed;
            attackAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (attackAction != null && attackAction.action != null)
        {
            attackAction.action.performed -= OnAttackPerformed;
            attackAction.action.Disable();
        }
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        SpawnSlash();
    }

    public void SpawnSlash()
    {
        if (slashEffectPrefab == null)
        {
            Debug.LogWarning("slashEffectPrefab is NULL", this);
            return;
        }

        bool facingRight = true;

        if (playerController != null)
            facingRight = playerController.FacingSign >= 0f;

        Vector3 spawnPosition;

        if (slashSpawnPoint != null)
        {
            spawnPosition = slashSpawnPoint.position;
        }
        else
        {
            float facingSign = facingRight ? 1f : -1f;
            spawnPosition = transform.position + new Vector3(
                slashOffset.x * facingSign,
                slashOffset.y,
                0f
            );
        }

        spawnPosition.z = 0f;

        Quaternion rotation = facingRight
            ? Quaternion.Euler(0f, 0f, -15f)
            : Quaternion.Euler(0f, 180f, 15f);

        GameObject slash = Instantiate(slashEffectPrefab, spawnPosition, rotation);

        PlayerSlashEffect effect = slash.GetComponent<PlayerSlashEffect>();
        if (effect != null)
            effect.SetFacing(facingRight);

        Debug.Log("Slash spawned", this);
    }
}