using Unity.AI.Navigation;
using Unity.VisualScripting;
using UnityEngine;

public class IslandSetup : MonoBehaviour
{
    public GameObject[] smallObjects;
    public GameObject[] largeObjects;
    
    public GameObject[] commonChestObjects;
    public GameObject[] rareChestObjects;
    public GameObject[] epicChestObjects;
    private GameObject[] usableChests;
    
    public Transform[] smallLocations;
    public Transform[] largeLocations;
    public Transform[] chestLocations;

    public NavMeshSurface surface;

    public GameObject playerPref; //used only if player doesnt already exist in scene
    public Transform[] playerSpawnPos;


    public bool common;
    public bool rare;
    public bool epic;
    void Start()
    {
        SpawnChests();
        SpawnObjects();
        //ResetNavmesh(); //needs to be fixed
        SpawnPlayer();
    }
    private void Awake()
    {
        SpawnPlayer();
    }
    void SpawnChests()
    {
        //Assign usable chests depending on rarity of island
        if(epic == true)
        {
            usableChests = epicChestObjects;
        }
        else if(rare == true)
        {
            usableChests = rareChestObjects;
        }
        else
        {
            usableChests = commonChestObjects;
        }


        for (int i = 0; i < chestLocations.Length; i++)
        {
            RaycastHit hit;
            int random = Random.Range(0, usableChests.Length);
            if (Physics.Raycast(chestLocations[i].position, Vector3.down, out hit))
            {
                
                Instantiate(usableChests[random], hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
            }
            else
            {
                Debug.Log("Chest Unable to raycast at chest location " + i);
            }
        }
    }

    void SpawnObjects()
    {
        //Spawn Small Objects
        for (int i = 0; i < smallLocations.Length; i++)
        {
            RaycastHit hit;
            if (Physics.Raycast(smallLocations[i].position, Vector3.down, out hit))
            {
                int random = Random.Range(0, smallObjects.Length);
                Instantiate(smallObjects[random], hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
            }
            else
            {
                Debug.Log("Unable to raycast small object at location " + i);
            }
        }
        
        //Spawn Large Objects
        for (int i = 0; i < largeLocations.Length; i++)
        {
            RaycastHit hit;
            if (Physics.Raycast(largeLocations[i].position, Vector3.down, out hit))
            {
                int random = Random.Range(0, largeObjects.Length);
                Instantiate(largeObjects[random], hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
            }
            else
            {
                Debug.Log("Unable to raycast Large object at location " + i);
            }
        }
    }

    void ResetNavmesh()
    {
        surface.BuildNavMesh();
    }
    void SpawnPlayer()
    {
        int random = Random.Range(0, playerSpawnPos.Length);
        if (GameObject.FindGameObjectWithTag("Player"))
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            player.transform.position = playerSpawnPos[random].position;
            Debug.Log("existing player spawned succesfully");
        }
        else
        {
            Instantiate(playerPref, playerSpawnPos[random].position, Quaternion.identity);
        }

    }
}
