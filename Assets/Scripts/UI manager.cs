using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class UImanager : MonoBehaviour
{
    public GameObject PauseScreen;
    public GameObject OptionsScreen;
    public GameObject MainPauseScreen;
    public GameObject DeathScreen;
    public GameObject WinScreen;
    public GameObject PlayerUIScreen;
    public GameObject OffhandWheelScreen;
    public GameObject UpgradeStationScreen;
    public GameObject lightTut;
    public GameObject curseTut;
    public GameObject fireballTut;
    public FirstPersonController firstPersonController;
    public bool ModalOpen { get; private set; }

    public void SetModal(bool open)
    {
        ModalOpen = open;
    }
    private void Awake()
    {
        OpenPlayerUIScreen();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.U)) //used for creating the trailer.
        {
            Debug.Log("Closing all ui");
            DisableScreens();
        }
    }
    public void OpenPauseScreen()
    {
        if (WinScreen.activeSelf) return;
        ForceCloseTutorials();
        PreSetup(true, true);
        OptionsScreen.SetActive(false);
        MainPauseScreen.SetActive(true);
        PauseScreen.SetActive(true);
    }
    public void ForceCloseTutorials()
    {
        lightTut.SetActive(false);
        curseTut.SetActive(false);
        fireballTut.SetActive(false);
        SetModal(false);
        if (firstPersonController != null)
        {
            firstPersonController.playerCanMove = true;
            firstPersonController.cameraCanMove = true;
        }
    }
    public void OpenDeathScreen()
    {
        if (WinScreen.activeSelf) return;
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
        if (WinScreen.activeSelf) return;
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
        firstPersonController.playerCanMove = false;
        firstPersonController.cameraCanMove = false;

    }
    public void openCurseTutorial()
    {
        SetModal(true);
        PreSetup(true, true);
        curseTut.SetActive(true);
        firstPersonController.playerCanMove = false;
        firstPersonController.cameraCanMove = false;
    }
    public void openFireballTut()
    {
        SetModal(true);
        PreSetup(true, true);
        fireballTut.SetActive(true);
        firstPersonController.playerCanMove = false;
        firstPersonController.cameraCanMove = false;
    }
    public void closeTut()
    {
        lightTut.SetActive(false);
        curseTut.SetActive(false);
        fireballTut.SetActive(false);
        SetModal(false);
        PreSetup(false, false);
        firstPersonController.playerCanMove = true;
        firstPersonController.cameraCanMove = true;
        OpenPlayerUIScreen();
    }

    public void OpenWinScreen()
    {
        PreSetup(true, true);
        WinScreen.SetActive(true);
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
        WinScreen.SetActive(false);
        PauseScreen.SetActive(false);
        DeathScreen.SetActive(false);
        PlayerUIScreen.SetActive(false);

        //lightTut.SetActive(false);
        //fireballTut.SetActive(false);
        //curseTut.SetActive(false);
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
            firstPersonController.cameraCanMove = true;
            firstPersonController.playerCanMove = true;
        }
    }
}
