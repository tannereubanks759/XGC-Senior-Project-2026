using System.Collections.Generic;
using NUnit.Framework.Interfaces;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.UI;

public class inventoryScript : MonoBehaviour
{
    //public List<GameObject> items = new List<GameObject>();
    public GameObject inventoryUI;
    public bool isOpen = false;
    
    public List<ItemData> items = new List<ItemData>();
    public  ItemData currentlyEquippingItemData;
    public equipScript equip;
    public Transform middleInventorySlot;
    public GameObject spawnPoint;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inventoryUI = GameObject.Find("NEW INV");
        equip = inventoryUI.GetComponentInChildren<equipScript>();
        inventoryUI.SetActive(false);
        middleInventorySlot = inventoryUI.transform.Find("bg/AssigningSpot/assignImageBG");
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.I)) 
        {
            toggleInv();
        }
    }
    public void toggleInv() 
    {
        if(isOpen) 
        {
            //check if theres a child obj if so drop.
            if(middleInventorySlot.childCount > 0) 
            {
                GameObject gameObject = middleInventorySlot.gameObject;
                objectIdentifier OI = gameObject.GetComponentInChildren<objectIdentifier>();
                Instantiate(OI.item.prefab, spawnPoint.transform.position, Quaternion.identity);
                Debug.Log("DROPPED from closing inv");
                Destroy(middleInventorySlot.GetChild(0).gameObject);
                
            }
            interactScript.current.HideAllKeyIcons();
        }
        if (!isOpen)
        {
            interactScript.current.RefreshKeyIcons();
        }
        inventoryUI.SetActive(!isOpen);

        setPauseLogic(!isOpen);
        isOpen = !isOpen;
    }
    public void addToInventory(ItemData itemToAdd, GameObject artifact)
    {
        //Debug.Log("Adding", itemToAdd);
        items.Add(itemToAdd);
        // do this in other script as well
        
        //send to another script
        currentlyEquippingItemData = itemToAdd;
        
        equip.uiAdder(currentlyEquippingItemData, artifact);
        //Debug.Log("Sent call");

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
            FirstPersonController.isPaused = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            FirstPersonController.isPaused = false;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
