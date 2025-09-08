using UnityEngine;
using UnityEngine.SceneManagement;

public class UI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public KeyCode PauseKey = KeyCode.Tab;

    public bool isPaused = false;
    public GameObject PauseScreen;
    public GameObject DeathScreen;
    public Camera SceneCamera;
    void Start()
    {
        Resume();
        SceneCamera.gameObject.SetActive(false);
        EnableCursor(false);
        DeathScreen.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(PauseKey))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    void Pause()
    {
        FirstPersonController.isPaused = true;
        PauseScreen.SetActive(true);
        isPaused = true;
        EnableCursor(true);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        FirstPersonController.isPaused = false;
        PauseScreen.SetActive(false);
        isPaused = false;
        EnableCursor(false);
        Time.timeScale = 1f;
    }

    public void ShowDeathScreen()
    {
        FirstPersonController.isPaused = true;
        DeathScreen.SetActive(true);
        EnableCursor(true);
        Time.timeScale = 0f;
    }

    void EnableCursor(bool enabled)
    {
        if (enabled)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }


    }

    public void LoadScene(string name)
    {
        SceneManager.LoadScene(name);
    }
    public void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

}
