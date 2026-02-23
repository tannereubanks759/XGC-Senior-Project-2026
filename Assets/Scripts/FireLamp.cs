using UnityEngine;

public class FireLamp : MonoBehaviour
{
    FireSourceScript fire;
    public GameObject text;
    public bool playerInRange = false;
    public bool hasCollected = false;
    public KeyCode collectKey = KeyCode.E;

    private FireballManager fm;
    private GameObject fireballParent;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerInRange = false;
        hasCollected = false;
        fire = GetComponentInChildren<FireSourceScript>();
        text.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(playerInRange && !hasCollected && Input.GetKeyDown(collectKey))
        {
            fire.isCollected = true;
            hasCollected = false;
            text.SetActive(false);
        }

    }
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && !fire.isCollected)
        {
            if (!fireballParent)
            {
                fireballParent = other.GetComponentInChildren<offhandHandler>().fireBall;
            }
            if (!fm)
            {
                fm = other.GetComponentInChildren<FireballManager>();
            }
            
            if (fireballParent.activeSelf && !fm.bothReadyOrRecharging)
            {
                playerInRange = true;
                text.SetActive(true);
            }
            else
            {
                playerInRange = false;
                text.SetActive(false);
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameObject fireballParent = other.GetComponentInChildren<offhandHandler>().fireBall;
            if (fireballParent.activeSelf)
            {
                playerInRange = false;
                text.SetActive(false);
            }
        }
    }
}
