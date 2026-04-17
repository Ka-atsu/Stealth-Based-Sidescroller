using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image[] healthSlots;

    [Header("Colors")]
    [SerializeField] private Color fullColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.55f);

    [Header("Scale")]
    [SerializeField] private Vector3 fullScale = Vector3.one;
    [SerializeField] private Vector3 emptyScale = new Vector3(0.9f, 0.9f, 1f);

    private int lastHealth = -1;

    private void Start()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        RefreshUI(true);
    }

    private void Update()
    {
        if (playerHealth == null)
            return;

        int currentHealth = playerHealth.GetCurrentHealth();

        if (currentHealth != lastHealth)
            RefreshUI();
    }

    public void RefreshUI(bool force = false)
    {
        if (playerHealth == null || healthSlots == null || healthSlots.Length == 0)
            return;

        int currentHealth = playerHealth.GetCurrentHealth();
        int maxHealth = playerHealth.GetMaxHealth();

        for (int i = 0; i < healthSlots.Length; i++)
        {
            if (healthSlots[i] == null)
                continue;

            bool isActive = i < currentHealth;
            bool isValidSlot = i < maxHealth;

            healthSlots[i].enabled = isValidSlot;

            if (!isValidSlot)
                continue;

            healthSlots[i].color = isActive ? fullColor : emptyColor;
            healthSlots[i].rectTransform.localScale = isActive ? fullScale : emptyScale;
        }

        lastHealth = currentHealth;
    }
}