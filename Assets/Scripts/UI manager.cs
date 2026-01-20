using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class UImanager : MonoBehaviour
{
    public GameObject PauseScreen;
    public GameObject OptionsScreen;
    public GameObject MainPauseScreen;
    public GameObject DeathScreen;
    public GameObject PlayerUIScreen;
    public GameObject OffhandWheelScreen;
    public GameObject UpgradeStationScreen;
    public GameObject lightTut;
    public GameObject curseTut;
    public GameObject fireballTut;
    public bool ModalOpen { get; private set; }

    public void SetModal(bool open)
    {
        ModalOpen = open;
    }
    private void Awake()
    {
        OpenPlayerUIScreen();
    }

    public void OpenPauseScreen()
    {
        PreSetup(true, true);
        OptionsScreen.SetActive(false);
        MainPauseScreen.SetActive(true);
        PauseScreen.SetActive(true);
    }
    public void OpenDeathScreen()
    {
        PreSetup(true, true);
        DeathScreen.SetActive(true);
    }
    public void CloseDeathScreen()
    {
        PreSetup(false, false);
        DeathScreen.SetActive(false);
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
    public void openLightTutorial()
    {
        SetModal(true);
        PreSetup(true, true);
        lightTut.SetActive(true);
        
    }
    public void openCurseTutorial()
    {
        SetModal(true);
        PreSetup(true, true);
        curseTut.SetActive(true);
    }
    public void openFireballTut()
    {
        SetModal(true);
        PreSetup(true, true);
        fireballTut.SetActive(true);
    }
    public void closeTut()
    {
        lightTut.SetActive(false);
        curseTut.SetActive(false);
        fireballTut.SetActive(false);
        SetModal(false);
        PreSetup(false, false);
       
        OpenPlayerUIScreen();
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
