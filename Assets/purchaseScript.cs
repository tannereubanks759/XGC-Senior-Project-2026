using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Events;
using static Unity.VisualScripting.Member;

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
    public bool isSecondUpgrade;
    [Header("Audio")]
    public AudioSource source;
    public AudioClip purchaseClip;
    public float purchaseVol = 0.8f;
    public AudioClip declineClip;
    public float declineVol = 0.8f;

    void Start()
    {
        //buttonToDisable = this.gameObject;
    }
    private void PlaySound(AudioClip clip, float vol = 1f)
    {
        if (source != null && clip != null)
            source.PlayOneShot(clip, vol);
    }
    public void purchase()
    {
        if (prevUpgrades != null && prevUpgrades.Length > 0)
        {
            if (isSecondUpgrade)
            {
                int owned = 0;
                foreach (var req in prevUpgrades)
                {
                    if (columnProgress.boughtUpgrades.Contains(req))
                        owned++;
                }
                if (owned < 2)
                {
                    PlaySound(declineClip, declineVol);
                    return;
                }
            }
            else
            {
                foreach (var req in prevUpgrades)
                {
                    if (!columnProgress.boughtUpgrades.Contains(req))
                    {
                        PlaySound(declineClip, declineVol);
                        return;
                    }
                }
            }
        }
        if (gb.gold < price)
        {
            PlaySound(declineClip, declineVol);
            return;
        }
        gb.RemoveGold(price);
        goldText.text = gb.gold.ToString();
        PlaySound(purchaseClip, purchaseVol);
        buttonToDisable.SetActive(false);
        tooltipUI.SetActive(false);

        if (isMajorUpgrade)
            offhandWheelUpdate();

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
