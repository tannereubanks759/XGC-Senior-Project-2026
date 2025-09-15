using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEngine;

public class ChestScript : MonoBehaviour
{
    //public List<GameObject> artifactPool;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public interactScript interactScript;
    public List<ItemData> possibleArtifacts;
    public GameObject spawnLocation;
    public ItemData itemGenerated;
    public infoscript infoScriptRef;
    private IEnumerator Start()
    {
        GameObject player = null;
        
        while (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            yield return null; 
        }
        interactScript = player.GetComponent<interactScript>();
        infoScriptRef = GameObject.Find("PlayerInfo").GetComponent<infoscript>();
    }
   
    public void chestOpen()
    {
        if(infoScriptRef.keyCount > 0) 
        {
            infoScriptRef.keyCount--;
            Debug.Log("Chest opnened");
            generate();
            //this.gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("Couldnt open do to keys: " + interactScript.keyCount);
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
    private void generate()
    {
        GameObject itg = null;
        int num = Random.Range(0, possibleArtifacts.Count);
        itemGenerated = possibleArtifacts[num];
        Debug.Log("Item Generated: " + itemGenerated.itemName);
        itg = Instantiate(itemGenerated.prefab, spawnLocation.transform);
        itg.transform.localPosition = Vector3.zero;
        itg.transform.localRotation = Quaternion.identity;
        itg.transform.localScale = Vector3.one;
        Debug.Log("Spawned");
    }
}
