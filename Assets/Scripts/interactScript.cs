using UnityEngine;
using TMPro;
using NUnit.Framework;
using System.Collections.Generic;

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
    void Start()
    {
        interactText = GameObject.Find("interactText");
        interactText.SetActive(false);
        infoScriptRef = GameObject.Find("PlayerInfo").GetComponent<infoscript>();
        //chest =  GameObject.Find("Animated PBR Chest _Wood_Demo").GetComponent<ChestScript>();
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
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Artifact"))
        {
            //currentArtifact = null;
            //currentArtifactObj = null;
            interactText.SetActive(false);
            canInteract = false;
            Debug.Log("Left artifact");
        }
        else if (other.CompareTag("Key"))
        {
            keyInteract = false;
            keyobj = null;
            interactText.SetActive(false);
            
        }
        else if (other.CompareTag("Chest"))
        {
            
            interactText.SetActive(false);
            chestInteract = false;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && canInteract && currentArtifact != null)
        {
            
            inventoryScript.addToInventory(currentArtifact);
            Destroy(currentArtifactObj);
            //Debug.Log("Added to inventory: " + currentArtifact.itemName);
            //objIdentifierRef.updateInfo(currentArtifact);
            interactText.SetActive(false);
            inventoryScript.toggleInv();
            canInteract = false;
        }
        else if (Input.GetKeyDown(KeyCode.E) && keyInteract) 
        {
            Destroy(keyobj);
            infoScriptRef.keyCount++;
            interactText.SetActive(false);
            keyInteract = false;
        }
        else if (Input.GetKeyDown(KeyCode.E) && chestInteract)
        {   
            interactText.SetActive(false);
            chestInteract = false;
            chest.chestOpen();
        }
    }
}
