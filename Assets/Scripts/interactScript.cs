using UnityEngine;
using TMPro;


public class interactScript : MonoBehaviour
{
    
    public GameObject interactText;
    public bool canInteract = false;
    public GameObject currentArtifactObj;
    public ItemData currentArtifact;
    public inventoryScript inventoryScript;

    void Start()
    {
        interactText.SetActive(false);
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
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Artifact"))
        {
            currentArtifact = null;
            currentArtifactObj = null;
            interactText.SetActive(false);
            canInteract = false;
            Debug.Log("Left artifact");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && canInteract && currentArtifact != null)
        {
            inventoryScript.addToInventory(currentArtifact);
            Destroy(currentArtifactObj);
            Debug.Log("Added to inventory: " + currentArtifact.itemName);
            interactText.SetActive(false);
            inventoryScript.toggleInv();
        }
    }
}
