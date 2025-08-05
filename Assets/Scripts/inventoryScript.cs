using System.Collections.Generic;
using NUnit.Framework.Interfaces;
using UnityEngine;

public class inventoryScript : MonoBehaviour
{
    public List<GameObject> items = new List<GameObject>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void addToInventory(GameObject itemToAdd)
    {
        Debug.Log("Adding", itemToAdd);
        items.Add(itemToAdd);
    }
}
