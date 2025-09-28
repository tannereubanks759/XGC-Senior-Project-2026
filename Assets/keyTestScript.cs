using UnityEngine;

public class keyTestScript : MonoBehaviour
{
    public GameObject chestAssigned;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ChestScript cs = chestAssigned.GetComponent<ChestScript>();
        cs.spawnKey(transform.position);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
