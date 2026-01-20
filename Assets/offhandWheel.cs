using UnityEngine;
using UnityEngine.Rendering;

public class offhandWheel : MonoBehaviour
{
    public UImanager UIM;
    private offhandHandler handler;
    private float currentPressTime;
    private float minHoldTime = 0.15f;
    private bool quickSwap = false;
    private bool wheelOpen = false;

    void Start()
    {
        handler = GameObject.FindAnyObjectByType<offhandHandler>();
    }

    public void openWheel()
    {
        if (wheelOpen) return;
        wheelOpen = true;
        UIM.OpenOffhandWheelScreen();
        var controllerRef = FindAnyObjectByType<FirstPersonController>();
        controllerRef.playerCanMove = false;
        controllerRef.cameraCanMove = false;
        Volume vol = Camera.main.gameObject.GetComponent<Volume>();
        vol.enabled = true;
        Time.timeScale = .3f;
    }

    public void closeWheel()
    {
        wheelOpen = false;
        var controllerRef = FindAnyObjectByType<FirstPersonController>();
        controllerRef.playerCanMove = true;
        controllerRef.cameraCanMove = true;
        var offhandHandle = FindAnyObjectByType<RMF_RadialMenu>();
        offhandHandle.buttonPress();
        Volume vol = Camera.main.gameObject.GetComponent<Volume>();
        vol.enabled = false;
        if (UIM != null && UIM.ModalOpen) return;
        UIM.OpenPlayerUIScreen();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            currentPressTime = Time.unscaledTime;
            quickSwap = false;
        }

        if (Input.GetKey(KeyCode.Q) && !quickSwap)
        {
            if (Time.unscaledTime - currentPressTime >= minHoldTime)
            {
                openWheel();
            }
        }

        if (Input.GetKeyUp(KeyCode.Q))
        {
            float pressDuration = Time.unscaledTime - currentPressTime;

            if (pressDuration < minHoldTime)
            {
                quickSwap = true;
                if (handler != null)
                {
                    handler.quickSwap();
                }
            }
            else
            {
                closeWheel();
            }
        }
    }
}