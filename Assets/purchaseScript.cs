using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Events;

public class purchaseScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject[] prevUpgrades;
    public columnProgress columnProgress;
    public GameObject buttonToDisable;
    public int price;
    public GoldBank gb;
    public GameObject offhandX;
    public GameObject offhandCheck;
    public TextMeshProUGUI goldText;
    public bool isMajorUpgrade;
    public UnityEvent onPurchaseSuccess;
    public GameObject tooltipUI;
    void Start()
    {
        //buttonToDisable = this.gameObject;
    }
    public void purchase()
    {
        // prereq check
        foreach (var req in prevUpgrades)
        {
            //if a previous upgrade is not present then return
            if (!columnProgress.boughtUpgrades.Contains(req))
            {
                return;
            }
        }
        if (gb.gold < price) return;
        gb.RemoveGold(price);
        goldText.text = gb.gold.ToString();
        buttonToDisable.SetActive(false);
        tooltipUI.SetActive(false);
        if (isMajorUpgrade)
        {
            offhandWheelUpdate();
        }
        columnProgress.boughtUpgrades.Add(gameObject);
        onPurchaseSuccess?.Invoke();
    }
    public void offhandWheelUpdate()
    {
        offhandX.SetActive(false);
        offhandCheck.SetActive(true);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
