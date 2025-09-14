using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public GameObject mainScreen;
    public GameObject loadingScreen;
    void Start()
    {
        Time.timeScale = 1f;
        EnableCursor(true);
        loadingScreen.SetActive(false);
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

    public void ExitGame()
    {
        Application.Quit();
    }
}
