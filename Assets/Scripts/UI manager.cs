using Unity.VisualScripting;
using UnityEngine;

public class UImanager : MonoBehaviour
{
    public GameObject PauseScreen;
    public GameObject OptionsScreen;
    public GameObject DeathScreen;
    public GameObject PlayerUIScreen;
    public GameObject OffhandWheelScreen;
    public GameObject UpgradeStationScreen;

    private void Awake()
    {
        OpenPlayerUIScreen();
    }
    public void OpenPauseScreen()
    {
        PreSetup(true, true);
        OptionsScreen.SetActive(false);
        PauseScreen.SetActive(true);
    }
    public void OpenDeathScreen()
    {
        PreSetup(true, true);
        DeathScreen.SetActive(true);
    }
    public void OpenPlayerUIScreen()
    {
        PreSetup(false, false);
        PlayerUIScreen.SetActive(true);
    }
    public void OpenOffhandWheelScreen()
    {
        PreSetup(true);
        Cursor.visible = false;
        OffhandWheelScreen.SetActive(true);
    }
    public void OpenUpgradeStationScreen()
    {
        PreSetup(true, true);
        UpgradeStationScreen.SetActive(true);
    }
    void PreSetup(bool hasCursor)
    {
        if (hasCursor)
        {
            EnableCursor(true);
        }
        else
        {
            EnableCursor(false);
        }
        DisableScreens();
    }
    void PreSetup(bool hasCursor, bool timePause)
    {
        if (hasCursor)
        {
            EnableCursor(true);
        }
        else
        {
            EnableCursor(false);
        }
        if (timePause)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
        }
        DisableScreens();
    }
    void DisableScreens()
    {
        PauseScreen.SetActive(false);
        DeathScreen.SetActive(false);
        PlayerUIScreen.SetActive(false);
        if(OffhandWheelScreen != null)
        {
            OffhandWheelScreen.SetActive(false);
        }
        if(UpgradeStationScreen != null)
        {
            UpgradeStationScreen.SetActive(false);
        }
        
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
}
