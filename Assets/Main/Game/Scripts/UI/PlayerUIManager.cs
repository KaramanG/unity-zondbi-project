using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerUIManager : MonoBehaviour
{
    [Header("Player Stats")]
    [SerializeField] private HealthSystem playerHealthSystem;
    [SerializeField] private ManaSystem playerManaSystem;

    [Header("UI Elements")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image manaBarFill;
    [SerializeField] private TextMeshProUGUI manaText;

    [Header("Game Over Logic")]
    [SerializeField] private MouseLock mouseLock;
    [SerializeField] private GameObject gameOverScreenPanel;

    private void Awake()
    {
        if (healthBarFill != null)
        {
            healthBarFill.type = Image.Type.Filled;
            healthBarFill.fillMethod = Image.FillMethod.Horizontal;
            healthBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
        if (manaBarFill != null)
        {
            manaBarFill.type = Image.Type.Filled;
            manaBarFill.fillMethod = Image.FillMethod.Horizontal;
            manaBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }

    private void Start()
    {
        gameOverScreenPanel.SetActive(false);

        if (playerHealthSystem == null || playerManaSystem == null)
        {
            enabled = false;
            return;
        }

        playerHealthSystem.OnHealthChanged.AddListener(UpdateHealthUI);
        playerHealthSystem.OnPlayerDied.AddListener(ShowGameOverScreen);
        UpdateHealthUI(playerHealthSystem.GetHealth(), playerHealthSystem.GetMaxHealth());

        playerManaSystem.OnManaChanged.AddListener(UpdateManaUI);
        UpdateManaUI(playerManaSystem.GetMana(), playerManaSystem.GetMaxMana());
    }

    private void OnDestroy()
    {
        playerHealthSystem.OnHealthChanged.RemoveListener(UpdateHealthUI);
        playerHealthSystem.OnPlayerDied.RemoveListener(ShowGameOverScreen);
        playerManaSystem.OnManaChanged.RemoveListener(UpdateManaUI);
    }

    public void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        float fillAmount = (maxHealth > 0) ? (currentHealth / maxHealth) : 0f;
        healthBarFill.fillAmount = fillAmount;
        healthText.text = $"{Mathf.CeilToInt(currentHealth)}";
    }
    public void UpdateManaUI(float currentMana, float maxMana)
    {
        float fillAmount = (maxMana > 0) ? (currentMana / maxMana) : 0f;
        manaBarFill.fillAmount = fillAmount;
        manaText.text = $"{Mathf.CeilToInt(currentMana)}";
    }

    private void ShowGameOverScreen()
    {
        mouseLock.ShowCursorLock = true;
        playerManaSystem.canRegenMana = false;
        gameOverScreenPanel.SetActive(true);
    }

    public void RestartGame()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }
}
