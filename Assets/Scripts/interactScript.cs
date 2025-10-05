using UnityEngine;
using TMPro;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;

public class interactScript : MonoBehaviour
{
    
    public GameObject interactText;
    private bool canInteract = false;
    public GameObject currentArtifactObj;
    public ItemData currentArtifact;
    public inventoryScript inventoryScript;
    private bool keyInteract = false;
    private bool chestInteract = false; 
    public int keyCount = 0;
    public GameObject keyobj;
    public ChestScript chest;
    public objectIdentifier objIdentifierRef;
    private infoscript infoScriptRef;
    private GameObject DungeonKey;
    public bool treasureRoomUnlocked = false;
    private GameObject dungeonDoor;
    private GameObject dungeonLock;
    public List<int> keyIDs = new List<int>();
    public static interactScript current; 
    
    public GameObject redKeyObj;
    public GameObject blueKey;
    public GameObject greenKey;
    public GameObject goldKey;
    void Start()
    {
        current = this;
        interactText = GameObject.Find("interactText");
        interactText.SetActive(false);
        infoScriptRef = GameObject.Find("PlayerInfo").GetComponent<infoscript>();
        HideAllKeyIcons();
        //chest =  GameObject.Find("Animated PBR Chest _Wood_Demo").GetComponent<ChestScript>();
    }
    private void Awake()
    {
        redKeyObj = GameObject.FindGameObjectWithTag("redKey");
        blueKey = GameObject.FindGameObjectWithTag("blueKey");
        greenKey = GameObject.FindGameObjectWithTag("greenKey");
        goldKey = GameObject.FindGameObjectWithTag("goldKey");
    }
    private void colorIDChecker(int keyID, bool active)
    {
        int key = (keyID - 1) % 4;
        switch (key)
        {
            case 0: 
                if (redKeyObj) redKeyObj.SetActive(active); 
                break;
            case 1: 
                if (greenKey) greenKey.SetActive(active); 
                break;
            case 2: 
                if (blueKey) blueKey.SetActive(active); 
                break;
            case 3: if (goldKey) goldKey.SetActive(active); 
                break;
        }
    }
    public void RefreshKeyIcons()
    {
        
       redKeyObj.SetActive(false);
       greenKey.SetActive(false);
       blueKey.SetActive(false);
       goldKey.SetActive(false);

        
        foreach (int id in keyIDs)
        {
            colorIDChecker(id, true);
        }
            
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Artifact"))
        {
            itemDataAssigner artifact = other.GetComponent<itemDataAssigner>();
            if (artifact != null)
            {
                currentArtifact = artifact.itemData;
                currentArtifactObj = other.gameObject;
                canInteract = true;

                if (interactText != null)
                    interactText.SetActive(true);

                Debug.Log("Touched artifact: " + currentArtifact.itemName);
            }
        }
        else if (other.CompareTag("Key"))
        {
            keyInteract = true;
            interactText.SetActive(true);
            keyobj = other.gameObject;
        }
        else if (other.CompareTag("Chest"))
        {

            interactText.SetActive(true);
            chest = other.GetComponent<ChestScript>();
            chestInteract = true;   
            
        }
        else if (other.CompareTag("DungeonKey"))
        {
            interactText.SetActive(true);
            DungeonKey = other.gameObject;
        }
        else if (other.CompareTag("DungeonLock"))
        {
            if (interactText != null)
                interactText.SetActive(true);
            dungeonLock = other.gameObject;
        }
        if (other.CompareTag("DungeonDoor") && treasureRoomUnlocked)
        {
            interactText.SetActive(true);
            dungeonDoor = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Artifact"))
        {
            //currentArtifact = null;
            //currentArtifactObj = null;
            canInteract = false;
            Debug.Log("Left artifact");
        }
        else if (other.CompareTag("Key"))
        {
            keyInteract = false;
            keyobj = null;
        }
        else if (other.CompareTag("Chest"))
        {
            chestInteract = false;
        }
        else if (other.CompareTag("DungeonKey"))
        {
            DungeonKey = null;
        }
        else if (other.CompareTag("DungeonLock"))
        {
            dungeonLock = null;
        }
        if (other.CompareTag("DungeonDoor"))
        {
            dungeonDoor = null;
        }
        interactText.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (canInteract && currentArtifact != null)
            {

                inventoryScript.addToInventory(currentArtifact, currentArtifactObj);
                //Destroy(currentArtifactObj);
                //Debug.Log("Added to inventory: " + currentArtifact.itemName);
                //objIdentifierRef.updateInfo(currentArtifact);
                
                inventoryScript.toggleInv();
                canInteract = false;
            }
            else if (keyInteract)
            {
                keyobj.SetActive(false);
                keyScript k = keyobj.GetComponent<keyScript>();
                int id = k.keyID;
                keyIDs.Add(id);
                infoScriptRef.keyCount++;
                k.chest.DisableSeal();
                k.chest.chestOutline.enabled = true;
                if (inventoryScript.inventoryUI.activeSelf)
                {
                    colorIDChecker(id, true);
                }
                keyInteract = false;
            }
            else if (chestInteract)
            {
                chestInteract = false;
                chest.chestOpen(current);
                int id = chest.keyID;
                colorIDChecker(id, false);
            }
            else if (dungeonLock != null)
            {
                GameObject.FindAnyObjectByType<TreasureRoomLockKey>().Unlock();
                dungeonLock = null;
            }
            else if(DungeonKey != null)
            {
                DungeonKey.GetComponentInParent<TreasureRoomLockKey>().PickupKey();
                DungeonKey = null;
            }
            else if(dungeonDoor != null && treasureRoomUnlocked)
            {
                Debug.Log("Open Door");
                //put door open animator here
                dungeonDoor.GetComponentInParent<Animator>().SetBool("DoorOpen", true);
                dungeonDoor = null;
            }
            

                interactText.SetActive(false);

        }
    }
    public void HideAllKeyIcons()
    {
        redKeyObj.SetActive(false);
        greenKey.SetActive(false);
        blueKey.SetActive(false);
        goldKey.SetActive(false);

    }
}
