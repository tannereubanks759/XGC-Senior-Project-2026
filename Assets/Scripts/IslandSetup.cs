using System;
using Unity.AI.Navigation;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

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

    [Header("Enemy Spawning Variables")]
    [Tooltip("The array of possible enemies to spawn")]
    public GameObject[] basicEnemies;
    [Tooltip("The radius in which the enemies will spawn around the chests")]
    public float spawnRadius = 10f;
    private RaycastHit[] raycastHit;


    public bool common;
    public bool rare;
    public bool epic;
    void Start()
    {
        // Sets the array instance
        raycastHit = new RaycastHit[0];

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
            int random = UnityEngine.Random.Range(0, usableChests.Length);
            if (Physics.Raycast(chestLocations[i].position, Vector3.down, out hit))
            {
                
                var chest = Instantiate(usableChests[random], hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
                chest.AddComponent<PatrolArea>();
            }
            else
            {
                Debug.Log("Chest Unable to raycast at chest location " + i);
            }

            Array.Resize(ref raycastHit, raycastHit.Length + 1);
            raycastHit[raycastHit.Length - 1] = hit;
        }

        // Spawn 5 enemies around each chest
        for (int i = 0; i < raycastHit.Length; i++)
        {
            GetRandSpawnPoint(raycastHit[i]);
            GetRandSpawnPoint(raycastHit[i]);
            GetRandSpawnPoint(raycastHit[i]);
            GetRandSpawnPoint(raycastHit[i]);
            GetRandSpawnPoint(raycastHit[i]);
        }
    }

    // Spawn a random enemy from the list of enemies
    void SpawnEnemy(NavMeshHit hit, int rand)
    {
        Instantiate(basicEnemies[rand], hit.position, Quaternion.identity);
    }

    // Get a random spawn point within a radius around the chest
    void GetRandSpawnPoint(RaycastHit _hit)
    {
        // Pick a random point inside a unit circle and scale by radius.
        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * spawnRadius;

        // Convert 2D circle to 3D world coordinates (Y stays the same as the object's position).
        Vector3 randomPoint = _hit.transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

        // Snap the random point to the NavMesh to ensure the enemy can reach it.
        if (NavMesh.SamplePosition(randomPoint, out var hit, 1f, NavMesh.AllAreas))
        {
            SpawnEnemy(hit, UnityEngine.Random.Range(0, basicEnemies.Length));
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
                int random = UnityEngine.Random.Range(0, smallObjects.Length);
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
                int random = UnityEngine.Random.Range(0, largeObjects.Length);
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
        int random = UnityEngine.Random.Range(0, playerSpawnPos.Length);
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
