using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class upgradeStationScript : MonoBehaviour
{
    public TextMeshProUGUI goldText;
    public UImanager UIM;
    public void closeUI()
    {
        UIM.OpenPlayerUIScreen();
    }
    
    
    public void openUI()
    {
        var goldScript = FindAnyObjectByType<GoldBank>();
        if(goldScript != null ) 
        { 
            goldText.text = goldScript.gold.ToString();
        }
        UIM.OpenUpgradeStationScreen();
    }
}
