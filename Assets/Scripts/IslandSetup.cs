using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEngine;

public class IslandSetup : MonoBehaviour
{
    
    public GameObject[] commonChestObjects;
    public GameObject[] rareChestObjects;
    public GameObject[] epicChestObjects;
    private GameObject[] usableChests;
    
    public Transform[] chestLocations;

    public NavMeshSurface surface;

    public GameObject playerPref; //used only if player doesnt already exist in scene
    public Transform[] playerSpawnPos;

    [Header("Enemy Spawning Variables")]
    [Tooltip("The array of possible enemies to spawn")]
    public GameObject[] basicEnemies;
    [Tooltip("The radius in which the enemies will spawn around the chests")]
    public float spawnRadius = 10f;
    [Tooltip("The amount of enemies that will spawn around each chest location")]
    public int enemiesPerChest = 5;

    public bool common;
    public bool rare;
    public bool epic;
    void Start()
    {
        SpawnChests();
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
                
                var chest = Instantiate(usableChests[random], hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
                chest.AddComponent<PatrolArea>();

                for (int j = 0; j < enemiesPerChest; j++) //Spawn enemies for each chest
                {
                    GetRandSpawnPoint(hit); 
                }
            }
            else
            {
                Debug.Log("Chest Unable to raycast at chest location " + i);
            }
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
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;

        // Convert 2D circle to 3D world coordinates (Y stays the same as the object's position).
        Vector3 randomPoint = _hit.point + new Vector3(randomCircle.x, 0f, randomCircle.y);

        // Snap the random point to the NavMesh to ensure the enemy can reach it.
        if (NavMesh.SamplePosition(randomPoint, out var hit, 5f, NavMesh.AllAreas))
        {
            SpawnEnemy(hit, Random.Range(0, basicEnemies.Length));
        }
    }

    

    void SpawnPlayer()
    {
        Vector3 spawnPos = Vector3.zero;
        int random = Random.Range(0, playerSpawnPos.Length);
        RaycastHit hit;
        if (Physics.Raycast(playerSpawnPos[random].position, Vector3.down, out hit))
        {
            spawnPos = hit.point;
        }

        
        if (GameObject.FindGameObjectWithTag("Player"))
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            player.transform.position = hit.point;
            Debug.Log("existing player spawned succesfully");
        }
        else
        {
            Instantiate(playerPref, hit.point, Quaternion.identity);
        }

    }
}
