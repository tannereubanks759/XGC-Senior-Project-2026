using UnityEngine;
using UnityEngine.Rendering;

public class offhandWheel : MonoBehaviour
{
    public GameObject offhandWheelCanvas;
   
    void Start()
    {
        offhandWheelCanvas.SetActive(false);
    }
    public void openWheel()
    {
        Volume vol = Camera.main.gameObject.GetComponent<Volume>();
        vol.enabled = true;
        offhandWheelCanvas.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = .3f;
    }
    public void closeWheel()
    {
        var offhandHandle = FindAnyObjectByType<RMF_RadialMenu>();
        offhandHandle.buttonPress();
        Volume vol = Camera.main.gameObject.GetComponent<Volume>();
        vol.enabled = false;
        offhandWheelCanvas.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1f;
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
