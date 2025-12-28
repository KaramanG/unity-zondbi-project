using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;

    private bool isSettingsActive;

    private void Start()
    {
        mainMenuPanel.SetActive(true);

        isSettingsActive = false;
        settingsPanel.SetActive(false);
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("LOCA");
        Time.timeScale = 1.0f;
    }

    public void ToggleSettings()
    {
        isSettingsActive = !isSettingsActive;

        mainMenuPanel.SetActive(!isSettingsActive);
        settingsPanel.SetActive(isSettingsActive);
    }
}