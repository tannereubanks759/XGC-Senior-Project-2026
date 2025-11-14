using UnityEngine;
using UnityEngine.Rendering;

public class offhandWheel : MonoBehaviour
{
    public UImanager UIM;
    void Start()
    {
    }
    public void openWheel()
    {
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
        var controllerRef = FindAnyObjectByType<FirstPersonController>();
        controllerRef.playerCanMove = true;
        controllerRef.cameraCanMove = true;
        var offhandHandle = FindAnyObjectByType<RMF_RadialMenu>();
        offhandHandle.buttonPress();
        Volume vol = Camera.main.gameObject.GetComponent<Volume>();
        vol.enabled = false;
        UIM.OpenPlayerUIScreen();
    }
    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Q)) 
        { 
            openWheel();
        }
        if (Input.GetKeyUp(KeyCode.Q))
        {
           closeWheel();
        }
    }
}
