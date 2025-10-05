using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class ChestScript : MonoBehaviour
{
    //public List<GameObject> artifactPool;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public interactScript interactScript;
    public List<ItemData> possibleArtifacts;
    public GameObject spawnLocation;
    public ItemData itemGenerated;
    public infoscript infoScriptRef;
    public GameObject keyRef;
    public GameObject lockedParticleEffect;
    private static int nextId = 1;
    public int keyID;
    public Transform KeySpawn; //Added this to reference position of where key should spawn
    public Animator hingeAnimator;
    public Outline chestOutline;
    public Color keyColor; 
    private Color[] palette = {
        new Color(0.9f, 0.2f, 0.2f),   
        new Color(0.2f, 0.7f, 0.25f),  
        new Color(0.2f, 0.45f, 0.9f),  
        new Color(0.95f, 0.8f, 0.2f)  
    };
    private void Awake()
    {
        if (keyID == 0)
        {
            keyID = nextId++;
        }
        keyColor = palette[(keyID - 1) % palette.Length];
        if (chestOutline != null)
        {
            chestOutline.OutlineColor = keyColor;
            chestOutline.enabled = false; 
        }
    }
    private void Start()
    {
        GameObject player = null;
        
        player = GameObject.FindGameObjectWithTag("Player");
        interactScript = player.GetComponent<interactScript>();
        infoScriptRef = GameObject.Find("PlayerInfo").GetComponent<infoscript>();

        spawnKey(KeySpawn.position); //Added this to spawn key at start
    }
    // CALL this on miniboss death
    public void spawnKey(Vector3 pos)
    {
        GameObject spawnedKey = Instantiate(keyRef, pos, Quaternion.identity);
        keyScript key = spawnedKey.GetComponent<keyScript>();
        key.keyID = keyID;
        key.chest = this;
        key.keyColor = keyColor;
    }
    public void chestOpen(interactScript player)
    {
        if (player.keyIDs.Contains(keyID))
        {
            player.keyIDs.Remove(keyID); 
            Debug.Log("Opened");
            hingeAnimator.SetTrigger("openChest");
            generate();
        }
        else
        {
            Debug.Log("Doesnt have key.");
        }
    }
    // Update is called once per frame
    void Update()
    {
        /*if(Input.GetKeyDown(KeyCode.K))
        {
            spawnKey(spawnLocation.transform.position);
        }*/
    }
    public void DisableSeal()
    {
        lockedParticleEffect.SetActive(false);
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
        
        // later add open animation maybe
    }
}
