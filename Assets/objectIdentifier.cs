using UnityEngine;
using UnityEngine.UI;

public class objectIdentifier : MonoBehaviour
{
    public ItemData item;
    public FirstPersonController firstPersonController;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void updateInfo(ItemData itemI)
    {
        item = itemI;
        //Debug.Log("New artifact: " + item.itemName + " on: " + gameObject.name);
        //item.isActive = isActive;
    }
}
