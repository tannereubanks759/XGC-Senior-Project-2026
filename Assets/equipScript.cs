using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
public class equipScript : MonoBehaviour
{
    public Image assigningSpotImage;
    public objectIdentifier objIdentifierRef;
    public GameObject midReferenceUISpot;
    public GameObject uiPrefab;
    //public GameObject player;
    public infoscript infoscriptRef;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        infoscriptRef =  FindAnyObjectByType<infoscript>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void uiAdder(ItemData itemAdded)
    {
        GameObject newUI = Instantiate(uiPrefab, midReferenceUISpot.transform);



        objIdentifierRef = GetComponentInChildren<objectIdentifier>();
        Image uiImage = newUI.GetComponent<Image>();
        if (uiImage != null)
        {
            uiImage.sprite = itemAdded.icon;
        }
        objIdentifierRef.updateInfo(itemAdded);
    }
    public void equip(ItemData itemAdded) 
    {
        
       
        //assigningSpotImage.sprite = itemAdded.icon;
        
        itemAdded.OnEquip(infoscriptRef.player);
        //call function from itemAdded that applies. 
    }
}
