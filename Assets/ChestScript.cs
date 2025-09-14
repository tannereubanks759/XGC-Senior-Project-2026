using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class ChestScript : MonoBehaviour
{
    //public List<GameObject> artifactPool;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public interactScript interactScript;
    public List<ItemData> possibleArtifacts;
    public GameObject spawnLocation;
    ItemData itemGenerated;
    void Start()
    {
        
    }
    public void chestOpen()
    {
        if(interactScript.keyCount > 0) 
        { 
            interactScript.keyCount--;
            print("Chest opened");
            generate();
            //this.gameObject.SetActive(false);
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
    private void generate()
    {
        int num = Random.Range(0, possibleArtifacts.Count);
        itemGenerated = possibleArtifacts[num];
        Debug.Log("Item Generated: " + itemGenerated.itemName);
        Instantiate(itemGenerated.prefab, spawnLocation.transform);
        //Debug.Log("Spawned");
    }
}
