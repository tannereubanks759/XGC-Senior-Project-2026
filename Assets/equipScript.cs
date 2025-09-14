using UnityEngine;
using UnityEngine.UI;
public class equipScript : MonoBehaviour
{
    public Image assigningSpotImage;
    public objectIdentifier objIdentifierRef;
    public GameObject midReferenceUISpot;
    public GameObject uiPrefab;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void equip(ItemData itemAdded) 
    {
        
        GameObject newUI = Instantiate(uiPrefab, midReferenceUISpot.transform);

        

        objIdentifierRef = GetComponentInChildren<objectIdentifier>();
        Image uiImage = newUI.GetComponent<Image>();
        if (uiImage != null)
        {
            uiImage.sprite = itemAdded.icon;
        }
        //assigningSpotImage.sprite = itemAdded.icon;
        objIdentifierRef.updateInfo(itemAdded);
        //call function from itemAdded that applies. 
    }
}
