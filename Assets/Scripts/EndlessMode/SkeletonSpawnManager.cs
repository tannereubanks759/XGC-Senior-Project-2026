using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SkeletonSpawnManager : MonoBehaviour
{
    [Header("General")]
    public Transform playerPos;
    public GameObject[] enemies;

    [Header("Wave Settings")]
    public int startingWave = 1;
    public float timeBetweenWaves = 6f;
    public int startingSpawnCount = 5;
    public int spawnCountIncrementPerWave = 2;
    public int maxAliveEnemies = 12;

    [Header("Spawn Distance")]
    public float spawnDistanceMin = 10f;
    public float spawnDistanceMax = 25f;
    public float spawnDistanceMaxIncreasePerWave = 1f;

    [Header("Spawn Rate")]
    public float startingSpawnRate = 2.5f;
    public float minimumSpawnRate = 0.4f;
    public float spawnRateDecreasePerWave = 0.1f;

    [Header("Spawn Chance - Base Weights")]
    public float basicSkeletonSpawnChance = 50f;
    public float fireSkeletonSpawnChance = 20f;
    public float waterSkeletonSpawnChance = 10f;
    public float gunEnemySpawnChance = 10f;

    [Header("Spawn Chance Scaling")]
    [Tooltip("How much special enemy weight increases every wave.")]
    public float specialWeightIncreasePerWave = 1.5f;

    [Tooltip("Basic enemy weight can slowly fall so later waves feel less repetitive.")]
    public float basicWeightDecreasePerWave = 1f;

    [Header("NavMesh")]
    public float navMeshSampleRadius = 4f;
    public float seaLevel = 0f;
    public int locationPickAttempts = 30;

    [Header("Debug")]
    public bool autoStart = true;
    public bool waveInProgress = false;

    private int waveCount;
    private int currentWaveSpawnTarget;
    private int spawnedThisWave;
    private float currentSpawnRate;
    private float nextSpawnTime;
    private float nextWaveStartTime;

    private readonly List<GameObject> aliveEnemies = new List<GameObject>();

    private void Start()
    {
        waveCount = Mathf.Max(1, startingWave);
        currentSpawnRate = startingSpawnRate;

        if (autoStart)
        {
            StartWave();
        }
    }

    private void Update()
    {
        if (playerPos == null)
            return;

        CleanupDeadEnemies();

        if (!waveInProgress)
        {
            if (Time.time >= nextWaveStartTime)
            {
                StartWave();
            }

            return;
        }

        bool waveFinishedSpawning = spawnedThisWave >= currentWaveSpawnTarget;
        bool allEnemiesDead = aliveEnemies.Count == 0;

        // If this wave has finished spawning and all spawned enemies are dead,
        // move to next wave after a short intermission.
        if (waveFinishedSpawning && allEnemiesDead)
        {
            EndWave();
            return;
        }

        // Keep spawning until this wave's target is reached,
        // but do not exceed max alive count.
        if (Time.time >= nextSpawnTime &&
            spawnedThisWave < currentWaveSpawnTarget &&
            aliveEnemies.Count < maxAliveEnemies)
        {
            SpawnEnemy();
        }
    }

    private void StartWave()
    {
        waveInProgress = true;
        spawnedThisWave = 0;

        currentWaveSpawnTarget = startingSpawnCount + ((waveCount - 1) * spawnCountIncrementPerWave);
        currentSpawnRate = Mathf.Max(
            minimumSpawnRate,
            startingSpawnRate - ((waveCount - 1) * spawnRateDecreasePerWave)
        );

        nextSpawnTime = Time.time + 1f;

        // Increase spawn radius over time so the arena feels more active.
        spawnDistanceMax += (waveCount > 1) ? spawnDistanceMaxIncreasePerWave : 0f;

        Debug.Log($"Wave {waveCount} started. Target Spawns: {currentWaveSpawnTarget}");
    }

    private void EndWave()
    {
        waveInProgress = false;
        nextWaveStartTime = Time.time + timeBetweenWaves;
        waveCount++;

        Debug.Log($"Wave cleared. Next wave starts in {timeBetweenWaves} seconds.");
    }

    public void ForceStartNextWave()
    {
        CleanupDeadEnemies();

        if (aliveEnemies.Count > 0)
            return;

        waveInProgress = false;
        nextWaveStartTime = Time.time;
    }

    private void SpawnEnemy()
    {
        if (!TryPickLocation(playerPos, spawnDistanceMin, spawnDistanceMax, seaLevel, out Vector3 location))
        {
            nextSpawnTime = Time.time + 0.5f;
            return;
        }

        GameObject prefab = PickEnemyForCurrentWave();
        if (prefab == null)
        {
            nextSpawnTime = Time.time + currentSpawnRate;
            return;
        }

        GameObject spawned = Instantiate(prefab, location, Quaternion.identity);
        aliveEnemies.Add(spawned);

        spawnedThisWave++;
        nextSpawnTime = Time.time + currentSpawnRate;
    }

    private void CleanupDeadEnemies()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            if (aliveEnemies[i] == null)
            {
                aliveEnemies.RemoveAt(i);
            }
        }
    }

    private bool TryPickLocation(Transform center, float minDistance, float maxDistance, float minHeight, out Vector3 result)
    {
        result = default;

        if (center == null)
            return false;

        Vector3 origin = center.position;
        float minDistanceSqr = minDistance * minDistance;
        float maxDistanceClamped = Mathf.Max(minDistance, maxDistance);

        for (int i = 0; i < locationPickAttempts; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minDistance, maxDistanceClamped);
            Vector3 candidate = origin + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if ((candidate - origin).sqrMagnitude < minDistanceSqr)
                continue;

            if (candidate.y < minHeight)
                candidate.y = minHeight;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                if (hit.position.y < minHeight)
                    continue;

                result = hit.position;
                return true;
            }
        }

        return false;
    }

    private GameObject PickEnemyForCurrentWave()
    {
        if (enemies == null || enemies.Length == 0)
            return null;

        GameObject basic = enemies.Length > 0 ? enemies[0] : null;
        GameObject fire = enemies.Length > 1 ? enemies[1] : null;
        GameObject water = enemies.Length > 2 ? enemies[2] : null;
        GameObject gun = enemies.Length > 3 ? enemies[3] : null;

        float basicWeight = Mathf.Max(5f, basicSkeletonSpawnChance - ((waveCount - 1) * basicWeightDecreasePerWave));
        float fireWeight = Mathf.Max(0f, fireSkeletonSpawnChance + ((waveCount - 1) * specialWeightIncreasePerWave));
        float waterWeight = Mathf.Max(0f, waterSkeletonSpawnChance + ((waveCount - 1) * specialWeightIncreasePerWave));
        float gunWeight = Mathf.Max(0f, gunEnemySpawnChance + ((waveCount - 1) * specialWeightIncreasePerWave));

        float totalWeight = 0f;
        if (basic != null) totalWeight += basicWeight;
        if (fire != null) totalWeight += fireWeight;
        if (water != null) totalWeight += waterWeight;
        if (gun != null) totalWeight += gunWeight;

        if (totalWeight <= 0f)
            return basic;

        float roll = Random.Range(0f, totalWeight);

        if (basic != null)
        {
            if (roll < basicWeight) return basic;
            roll -= basicWeight;
        }

        if (fire != null)
        {
            if (roll < fireWeight) return fire;
            roll -= fireWeight;
        }

        if (water != null)
        {
            if (roll < waterWeight) return water;
            roll -= waterWeight;
        }

        if (gun != null)
        {
            if (roll < gunWeight) return gun;
        }

        return basic;
    }

    public int GetAliveEnemyCount()
    {
        CleanupDeadEnemies();
        return aliveEnemies.Count;
    }

    public int GetCurrentWave()
    {
        return waveCount;
    }

    public int GetRemainingSpawnsThisWave()
    {
        return Mathf.Max(0, currentWaveSpawnTarget - spawnedThisWave);
    }
}