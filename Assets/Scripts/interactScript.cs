using UnityEngine;
using TMPro; 


public class interactScript : MonoBehaviour
{
    public GameObject interactText;
    public bool canInteract = false;
    public GameObject currentArtifact;
    public inventoryScript inventoryScript;
    void Start()
    {
        interactText.SetActive(false);
    }
    private void OnTriggerStay(Collider other)
    {
        //Debug.Log("Detected anything");
        if (other.gameObject.CompareTag("Artifact"))
        {
            currentArtifact = other.gameObject;
            Debug.Log("Touched artifact");
            interactText.SetActive(true);
            canInteract = true;
           
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Artifact"))
        {
            Debug.Log("Left artifact");
            interactText.SetActive(false);
            canInteract=false;
        }

    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)&& canInteract == true)
        {
            inventoryScript.addToInventory(currentArtifact);
            currentArtifact.SetActive(false);
            Debug.Log("Adding to inv");
            interactText.SetActive(false);
        }
    }
}
