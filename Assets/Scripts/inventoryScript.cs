using System.Collections.Generic;
using NUnit.Framework.Interfaces;
using UnityEngine;

public class inventoryScript : MonoBehaviour
{
    public List<GameObject> items = new List<GameObject>();
    public GameObject inventoryUI;
    public bool isOpen = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab)) 
        {
            inventoryUI.SetActive(!isOpen);
            isOpen = !isOpen;
        }
    }
    public void addToInventory(GameObject itemToAdd)
    {
        //Debug.Log("Adding", itemToAdd);
        items.Add(itemToAdd);
    }
    public void removeInventory(GameObject itemToRemove) 
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
}
