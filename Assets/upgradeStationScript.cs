using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class upgradeStationScript : MonoBehaviour
{
    public TextMeshProUGUI goldText;
    private bool isOpen = false;
    public UImanager UIM;
    public void closeUI()
    {
        isOpen = false;
        UIM.OpenPlayerUIScreen();
    }
    
    
    public void openUI()
    {
        isOpen = true;
        var goldScript = FindAnyObjectByType<GoldBank>();
        if(goldScript != null ) 
        { 
            goldText.text = goldScript.gold.ToString();
        }
        UIM.OpenUpgradeStationScreen();
    }
}
