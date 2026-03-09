using UnityEngine;
using System.Collections.Generic;
using DigitalRuby.ThunderAndLightning;
using TMPro;

public class interactScript : MonoBehaviour
{
    
    public GameObject interactText;
    public GameObject teleporterInteractText;
    private TextMeshProUGUI tmpro;
    private bool canInteract = false;
    public GameObject currentArtifactObj;
    private GameObject currentHealthPotion;
    public ItemData currentArtifact;
    public inventoryScript inventoryScript;
    public objectIdentifier objIdentifierRef;
    private infoscript infoScriptRef;
    public static interactScript current;
    private bool shopInteract = false;
    private GameObject shop;
    private bool healthPotionInteract = false;
    public HealthPotion HealthPotionScript;
    private GoldBank goldRef;
    public int priceOfHealthPotion = 0;
    private bool upgrade = false;
    public bool TeleporterInteract = false;
    public GameObject currentUpgradeStation;
    public upgradeStationScript upgradeScript;
    void Start()
    {
        TeleporterInteract = false;
        current = this;
        interactText = GameObject.Find("interactText");
        teleporterInteractText.SetActive(false);
        interactText.SetActive(false);
        tmpro = interactText.GetComponent<TextMeshProUGUI>();
        goldRef = current.GetComponent<GoldBank>();
    }
    
    
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Artifact"))
        {
            itemDataAssigner artifact = other.GetComponent<itemDataAssigner>();
            if (artifact != null && artifact.wasOwned == true)
            {
                currentArtifact = artifact.itemData;
                currentArtifactObj = other.gameObject;
                tmpro.text = "E to interact";
                canInteract = true;
                interactText.SetActive(true);
            }
            else if (artifact != null)
            {

                currentArtifact = artifact.itemData;
                currentArtifactObj = other.gameObject;
                tmpro.text = "E to interact. Price: " + currentArtifact.price.ToString();
                canInteract = true;

               
                interactText.SetActive(true);    

                Debug.Log("Touched artifact: " + currentArtifact.itemName);
            }
        }
        else if (other.CompareTag("shop"))
        {
            shopInteract = true;
            interactText.SetActive(true);
            shop = other.gameObject;
            
        }
        else if (other.CompareTag("healthPotion"))
        {
            healthPotionInteract = true;
            tmpro.text = "E to pickup potion";
            interactText.SetActive(true);
            currentHealthPotion = other.gameObject;
        }
        else if (other.CompareTag("Teleporter"))
        {
            TeleporterInteract = true;
            teleporterInteractText.SetActive(true);
        }
        else if (other.CompareTag("Upgrade"))
        {
            upgrade = true;
            currentUpgradeStation = other.gameObject;
            tmpro.text = "Upgrade Station";
            interactText.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Artifact"))
        {
            //currentArtifact = null;
            //currentArtifactObj = null;
            canInteract = false;
            interactText.GetComponent<TextMeshProUGUI>().text = "E to interact";
            Debug.Log("Left artifact");
        }
        
        else if (other.CompareTag("healthPotion"))
        {
            healthPotionInteract = false;
            tmpro.text = "E to interact";
            currentHealthPotion = null;
        }
        
        else if (other.CompareTag("Teleporter"))
        {
            TeleporterInteract = false;
            teleporterInteractText.SetActive(false);
        }
        else if (other.CompareTag("shop"))
        {
            shopInteract = false;
            tmpro.text = "E to interact";
        }
        else if(other.CompareTag("Upgrade"))
        {
            upgrade = false;
            currentUpgradeStation = null;
            tmpro.text = "E to interact";
        }
        
        if(interactText != null)
        {
            interactText.SetActive(false);
        }
        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && interactText != null)
        {
            if (canInteract && currentArtifact != null)
            {
                var itemDataA = currentArtifactObj.GetComponent<itemDataAssigner>();
                int cost = itemDataA != null ? itemDataA.CurrentPrice : currentArtifact.price;
                
                if (goldRef.gold >= cost)
                {
                    goldRef.RemoveGold(cost);
                    if (itemDataA) itemDataA.wasOwned = true;
                    //inventoryScript.addToInventory(currentArtifact, currentArtifactObj);

                    //inventoryScript.toggleInv();
                    canInteract = false;
                }
            }
            else if (TeleporterInteract)
            {
                teleporterInteractText.SetActive(false);
                GameObject.FindAnyObjectByType<IslandTeleporter>().Teleport();
                TeleporterInteract = false;
            }
            else if (shopInteract)
            {
                var shopScript = shop.GetComponent<baseShop>();
            }
            else if (healthPotionInteract)
            {
                //if (goldRef.gold >= priceOfHealthPotion)
                //{
                    if (HealthPotionScript.GetQuantity() <= 4)
                    {
                        HealthPotionScript.CollectHealthPotion();
                        //goldRef.RemoveGold(priceOfHealthPotion);

                        if (currentHealthPotion != null)
                            Destroy(currentHealthPotion);

                        currentHealthPotion = null;
                        healthPotionInteract = false;
                        tmpro.text = "E to interact"; 
                    }
                else
                {
                    GetComponentInChildren<PopUpMessage>().ShowMessage("Can't carry any more potions");
                }
                //}
            }
            else if(upgrade)
            {
                if(currentUpgradeStation!=null)
                {
                    
                    upgradeScript.openUI();
                }
            }
            


                interactText.SetActive(false);

        }
    }
    
}
