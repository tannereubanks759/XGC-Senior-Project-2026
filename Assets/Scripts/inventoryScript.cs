using System.Collections.Generic;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.UI;

public class inventoryScript : MonoBehaviour
{
    //public List<GameObject> items = new List<GameObject>();
    public GameObject inventoryUI;
    public bool isOpen = false;
    public Image assigningSpotImage;
    public List<ItemData> items = new List<ItemData>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab)) 
        {
            toggleInv();
        }
    }
    public void toggleInv() 
    {
        inventoryUI.SetActive(!isOpen);
        setPauseLogic(!isOpen);
        isOpen = !isOpen;
    }
    public void addToInventory(ItemData itemToAdd)
    {
        //Debug.Log("Adding", itemToAdd);
        items.Add(itemToAdd);
        assigningSpotImage.sprite = itemToAdd.icon;
    }
    public void removeInventory(ItemData itemToRemove) 
    {
        items.Remove(itemToRemove);
    }
    public void updateUI(GameObject itemBeingUpdated, bool isAdd)
    {
        if(isAdd)
        {
            //logic for adding to ui
        }
        else
        {
            // logic for removing from ui
        }
    }
    public void setPauseLogic(bool pause)
    {
        if(pause) 
        { 
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
