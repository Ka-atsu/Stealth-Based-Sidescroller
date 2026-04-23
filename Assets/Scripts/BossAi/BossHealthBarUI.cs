using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BossAI2D bossAI;
    [SerializeField] private Image fillImage;
    [SerializeField] private GameObject rootObject;
    [SerializeField] private TMP_Text bossNameText;

    [Header("Settings")]
    [SerializeField] private string bossDisplayName = "Boss";

    private void Awake()
    {
        if (rootObject == null)
            rootObject = gameObject;

        if (bossAI == null)
            bossAI = Object.FindFirstObjectByType<BossAI2D>();

        if (bossNameText != null)
            bossNameText.text = bossDisplayName;

        Hide();
    }

    private void OnEnable()
    {
        if (bossAI != null)
            bossAI.OnStateChanged += HandleBossStateChanged;
    }

    private void OnDisable()
    {
        if (bossAI != null)
            bossAI.OnStateChanged -= HandleBossStateChanged;
    }

    private void Update()
    {
        if (bossAI == null || fillImage == null)
            return;

        fillImage.fillAmount = bossAI.HealthNormalized;
    }

    private void HandleBossStateChanged(BossAI2D.BossState newState)
    {
        if (newState == BossAI2D.BossState.Dead)
            Hide();
    }

    public void Show()
    {
        if (rootObject != null)
            rootObject.SetActive(true);
    }

    public void Hide()
    {
        if (rootObject != null)
            rootObject.SetActive(false);
    }
}