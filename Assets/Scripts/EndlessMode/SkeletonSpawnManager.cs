using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SkeletonSpawnManager : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnEntry
    {
        [Header("Enemy")]
        public string enemyName;
        public GameObject prefab;

        [Header("Weighting")]
        [Tooltip("Base chance weight for this enemy.")]
        public float baseWeight = 10f;

        [Tooltip("How much this enemy's weight changes every wave after wave 1.")]
        public float weightChangePerWave = 0f;

        [Tooltip("Minimum wave this enemy is allowed to spawn on.")]
        public int minWave = 1;

        [Tooltip("If true, this enemy can be spawned by the manager.")]
        public bool enabled = true;

        public float GetWeightForWave(int wave)
        {
            if (!enabled || prefab == null || wave < minWave)
                return 0f;

            float weight = baseWeight + ((wave - 1) * weightChangePerWave);
            return Mathf.Max(0f, weight);
        }
    }

    [Header("General")]
    public Transform playerPos;

    [Tooltip("Add as many enemy types as you want here.")]
    public List<EnemySpawnEntry> enemyTypes = new List<EnemySpawnEntry>();

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

    [Header("NavMesh")]
    public float navMeshSampleRadius = 4f;
    public float seaLevel = 0f;
    public int locationPickAttempts = 30;

    [Header("Debug")]
    public bool autoStart = true;
    public bool waveInProgress = false;
    public bool logChosenEnemy = false;

    private int waveCount;
    private int currentWaveSpawnTarget;
    private int spawnedThisWave;
    private float currentSpawnRate;
    private float nextSpawnTime;
    private float nextWaveStartTime;

    private float currentSpawnDistanceMax;
    private readonly List<GameObject> aliveEnemies = new List<GameObject>();

    private void Start()
    {
        waveCount = Mathf.Max(1, startingWave);
        currentSpawnRate = startingSpawnRate;
        currentSpawnDistanceMax = spawnDistanceMax;

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

        if (waveFinishedSpawning && allEnemiesDead)
        {
            EndWave();
            return;
        }

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

        currentSpawnDistanceMax = spawnDistanceMax + ((waveCount - 1) * spawnDistanceMaxIncreasePerWave);

        nextSpawnTime = Time.time + 1f;

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
        if (!TryPickLocation(playerPos, spawnDistanceMin, currentSpawnDistanceMax, seaLevel, out Vector3 location))
        {
            nextSpawnTime = Time.time + 0.5f;
            return;
        }

        GameObject prefab = PickEnemyForCurrentWave();
        if (prefab == null)
        {
            Debug.LogWarning("SkeletonSpawnManager: No valid enemy prefab could be picked for this wave.");
            nextSpawnTime = Time.time + currentSpawnRate;
            return;
        }

        GameObject spawned = Instantiate(prefab, location, Quaternion.identity);

        //autodetect code
        SkeletonGunEnemy gunEnemy = spawned.GetComponent<SkeletonGunEnemy>();
        SkeletonSwordEnemy swordEnemy = spawned.GetComponent<SkeletonSwordEnemy>();
        SkeletonBombEnemy bombEnemy = spawned.GetComponent<SkeletonBombEnemy>();
        if (gunEnemy)
        {
            gunEnemy.visionRange = 100;
            gunEnemy.autoDetectRange = 100;
        }
        else if (swordEnemy)
        {
            swordEnemy.visionRange = 100;
            swordEnemy.autoDetectRange = 100;
        }
        else if (bombEnemy)
        {
            bombEnemy.visionRange = 100;
            bombEnemy.autoDetectRange = 100;
        }

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
        if (enemyTypes == null || enemyTypes.Count == 0)
            return null;

        float totalWeight = 0f;

        for (int i = 0; i < enemyTypes.Count; i++)
        {
            if (enemyTypes[i] == null)
                continue;

            totalWeight += enemyTypes[i].GetWeightForWave(waveCount);
        }

        if (totalWeight <= 0f)
            return null;

        float roll = Random.Range(0f, totalWeight);

        for (int i = 0; i < enemyTypes.Count; i++)
        {
            EnemySpawnEntry entry = enemyTypes[i];
            if (entry == null)
                continue;

            float weight = entry.GetWeightForWave(waveCount);
            if (weight <= 0f)
                continue;

            if (roll < weight)
            {
                if (logChosenEnemy)
                {
                    Debug.Log($"Spawned enemy: {entry.enemyName} | Wave: {waveCount} | Weight: {weight}");
                }

                return entry.prefab;
            }

            roll -= weight;
        }

        for (int i = 0; i < enemyTypes.Count; i++)
        {
            if (enemyTypes[i] != null && enemyTypes[i].prefab != null)
                return enemyTypes[i].prefab;
        }

        return null;
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

    public float GetWeightForEnemyAtIndex(int index)
    {
        if (enemyTypes == null || index < 0 || index >= enemyTypes.Count || enemyTypes[index] == null)
            return 0f;

        return enemyTypes[index].GetWeightForWave(waveCount);
    }
}