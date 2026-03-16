using UnityEngine;
using UnityEngine.SceneManagement;

public class UI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public KeyCode PauseKey = KeyCode.Tab;
    public bool isPaused = false;
    public Camera SceneCamera;
    public AmbientZoneBlender amb;
    public CombatController combatController;
    public WeaponsManager wm;
    public UImanager UIM;
    public GameObject deathUI;
    public levelSavingManager levelSavingManager;
    public Animator HealthAnim;

    void Start()
    {
        Resume();
        EnableCursor(false);
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
        amb.PauseAmbience();
        combatController.isPaused = true;
        FirstPersonController.isPaused = true;
        isPaused = true;
        UIM.OpenPauseScreen();
    }

    public void Resume()
    {
        wm.isPaused = false;
        amb.UnpauseAmbience();
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
        if (name == "MainMenu")
        {
            Destroy(this.GetComponentInParent<FirstPersonController>().gameObject);
        }
        SceneManager.LoadScene(name);
    }
    public void RestartScene()
    {
       ResetPlayer();
       LoadScene(SceneManager.GetActiveScene().name);
    }
    void ResetPlayer()
    {
        FirstPersonController player = GetComponentInParent<FirstPersonController>();
        CombatController health = player.GetComponentInChildren<CombatController>();
        HealthAnim.SetTrigger("Dead");
        wm.healing = false;
        wm.healthPotion.SetActive(false);
        wm.weapons[wm.currentWeapon].SetActive(true);
        if (health != null)
        {
            health.health = health.maxHealth;
            health.healthSlider.value = health.health;
        }
        UIM.CloseDeathScreen();
        UIM.OpenPlayerUIScreen();
        player.playerCanMove = true;
        FirstPersonController.isPaused = false;
        levelSavingManager.RestoreToLevelStart();
    }
}
