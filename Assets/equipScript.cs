using UnityEngine;
using UnityEngine.UI;
public class equipScript : MonoBehaviour
{
    public Image assigningSpotImage;
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
        assigningSpotImage.sprite = itemAdded.icon;
        //call function from itemAdded that applies. 
    }
}
