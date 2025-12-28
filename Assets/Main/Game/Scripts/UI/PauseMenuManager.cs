using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    [SerializeField] private MouseLock mouseLock;
    [SerializeField] private GameObject pauseScreen;

    private bool isGamePaused;

    private void Start()
    {
        pauseScreen.SetActive(false);
        isGamePaused = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isGamePaused) PauseGame();
            else ResumeGame();
        }
    }

    public void PauseGame()
    {
        isGamePaused = true;
        mouseLock.ShowCursorLock = true;
        pauseScreen.SetActive(true);

        Time.timeScale = 0f;
    }
    public void ResumeGame()
    {
        isGamePaused = false;
        mouseLock.ShowCursorLock = false;
        pauseScreen.SetActive(false);

        Time.timeScale = 1f;
    }
    public void ExitToMenu()
    {
        SceneManager.LoadScene("MainMenu");
        //Time.timeScale = 1f;
    }
}
