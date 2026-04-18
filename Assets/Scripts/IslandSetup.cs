using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEngine;
using System.Collections.Generic;

public class IslandSetup : MonoBehaviour
{

    [Header("Player Spawn")]
    public GameObject playerPref; 
    public Transform[] playerSpawnPos;

    private void Awake()
    {
        SpawnPlayer();
    }

    void SpawnPlayer()
    {
        if (playerSpawnPos == null || playerSpawnPos.Length == 0)
        {
            Debug.LogWarning("No player spawn transforms assigned.");
            return;
        }

        int random = Random.Range(0, playerSpawnPos.Length);
        if (!Physics.Raycast(playerSpawnPos[random].position, Vector3.down, out var hit))
        {
            Debug.LogWarning("Player spawn raycast failed; placing at raw transform position.");
            hit.point = playerSpawnPos[random].position;
        }

        var existing = GameObject.FindGameObjectWithTag("Player");
        if (existing)
        {
            existing.transform.position = hit.point;
            existing.transform.rotation = playerSpawnPos[0].rotation;
            Debug.Log("Existing player moved to spawn.");
        }
        else
        {
            GameObject player = Instantiate(playerPref, hit.point, playerSpawnPos[0].transform.rotation);
        }

        
    }
}
