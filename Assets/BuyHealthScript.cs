using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuyHealthScript : MonoBehaviour
{
    public HealthPotion potion;
    public TextMeshProUGUI txt;
    public TextMeshProUGUI txt2;
    
    public GoldBank bank;
    public AudioSource source;
    public AudioClip buyClip;
    public AudioClip lowFundClip;
    public int price = 5;
    private void OnEnable()
    {
        txt.text = potion.GetQuantity().ToString();
    }
    
    public void BuyHealth()
    {
        if(bank.gold >= price && potion.GetQuantity() < 5)
        {
            bank.RemoveGold(3);
            potion.CollectHealthPotion();
            txt.text = potion.GetQuantity().ToString();
            txt2.text = potion.GetQuantity().ToString();
            //source.PlayOneShot(buyClip);
        }
        else
        {
            source.PlayOneShot(lowFundClip);
        }
    }
}
