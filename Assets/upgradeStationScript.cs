using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class upgradeStationScript : MonoBehaviour
{
    public GameObject upgradeUIScreen;
    public TextMeshProUGUI goldText;
    private bool isOpen = false;
    public void closeUI()
    {
        upgradeUIScreen.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isOpen = false;
    }
    void Start()
    {
        upgradeUIScreen.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        /*if(isOpen)
        {
            if(Input.GetKeyDown(KeyCode.Escape)) 
            { 
              closeUI();
            }
        }*/
    }
    public void openUI()
    {
        upgradeUIScreen.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        var goldScript = FindAnyObjectByType<GoldBank>();
        if(goldScript != null ) 
        { 
            goldText.text = goldScript.gold.ToString();
        }
    }
}
