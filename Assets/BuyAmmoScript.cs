using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuyAmmoScript : MonoBehaviour
{
    public Blunderbuss gun;
    public TextMeshProUGUI txt;
    public TextMeshProUGUI txt2;
    
    public GoldBank bank;
    public AudioSource source;
    public AudioClip ammoClip;
    public AudioClip lowFundClip;
    private void OnEnable()
    {
        txt.text = "x" + gun.totalAmmo;
    }
    
    public void BuyAmmo()
    {
        if(bank.gold >= 3)
        {
            bank.RemoveGold(3);
            
            gun.totalAmmo += 1;
            txt.text = "x" + gun.totalAmmo;
            txt2.text = "x" + gun.totalAmmo;
            source.PlayOneShot(ammoClip);
        }
        else
        {
            
            source.PlayOneShot(lowFundClip);
            
        }
    }
    
    
}
