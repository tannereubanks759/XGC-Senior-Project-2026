using UnityEngine;
using UnityEngine.SceneManagement;

public class UI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public KeyCode PauseKey = KeyCode.Tab;
    public bool isPaused = false;
    public GameObject PauseScreen;
    public GameObject DeathScreen;
    public GameObject playerUI;
    public Camera SceneCamera;
    public Ambience amb;
    public CombatController combatController;
    public WeaponsManager wm;
    public UImanager UIM;
    void Start()
    {
        Resume();
        //SceneCamera.gameObject.SetActive(false);
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
        wm.isPaused = true;
        amb.paused = true;
        combatController.isPaused = true;
        FirstPersonController.isPaused = true;
        isPaused = true;
        UIM.OpenPauseScreen();
    }

    public void Resume()
    {
        wm.isPaused = false;
        amb.paused = false;
        combatController.isPaused = false;
        FirstPersonController.isPaused = false;
        isPaused = false;
        UIM.OpenPlayerUIScreen();
    }

    public void ShowDeathScreen()
    {
        FirstPersonController.isPaused = true;
        UIM.OpenDeathScreen();
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
        if(name != "MainMenu" && name != SceneManager.GetActiveScene().name)
        {
            SceneManager.LoadScene(name);
        }
        else
        {
            Destroy(this.GetComponentInParent<FirstPersonController>().gameObject);
            SceneManager.LoadScene(name);
        }
        
    }
    public void RestartScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

}
