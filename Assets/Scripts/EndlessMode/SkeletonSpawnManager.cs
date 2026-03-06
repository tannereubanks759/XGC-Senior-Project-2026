using UnityEngine;
using UnityEngine.AI;

public class SkeletonSpawnManager : MonoBehaviour
{
    [Header("General")]
    public Transform playerPos;
    public GameObject[] Enemies;
    private int waveCount = 1;
    

    [Header("Spawn Distance")]
    public float spawnDistanceMin = 5;
    public float spawnDistanceMax = 20;

    [Header("Spawn Rate")]
    public float skeletonSpawnRateMinimum = 0.5f;
    public float skeletonSpawnRate = 3f;

    [Header("Spawn Count")]
    public int skeletonSpawnCount = 5;
    public int skeletonSpawnCountIncrement = 1;
    private int spawnedInWave = 0;

    [Header("Spawn Chance")]
    public float basicSkeletonSpawnChance = 50;
    public float fireSkeletonSpawnChance = 20;
    public float waterSkeletonSpawnChance = 10;
    public float gunEnemySpawnChance = 10;


    //private variables
    private Transform _pickHelper;
    private float NextSpawnTime = 0;
    

    // Update is called once per frame
    void Update()
    {
        if (!playerPos) return;

        if(NextSpawnTime < Time.time)
        {
            if(spawnedInWave < skeletonSpawnCount)
            {
                SpawnSkeleton();
            }
            else
            {
                NextSpawnTime = Time.time + 100;
            }
        }
    }

    public void IncrimentWave()
    {
        if(skeletonSpawnRate > skeletonSpawnRateMinimum)
        {
            skeletonSpawnRate -= 0.1f;
        }
        spawnDistanceMax += 1;
        spawnedInWave = 0;
        waveCount++;
        skeletonSpawnCount += skeletonSpawnCountIncrement;

    }
    public void SpawnSkeleton()
    {
        if (!TryPickLocation(playerPos, spawnDistanceMin, spawnDistanceMax, 0f, out Vector3 location))
            return;

        var prefab = PickSkeletonSafe();
        if (!prefab) return;

        Instantiate(prefab, location, Quaternion.identity);
        spawnedInWave++;
        NextSpawnTime = Time.time + skeletonSpawnRate;
    }

    private bool TryPickLocation(Transform center, float minDistance, float maxDistance, float seaLevel, out Vector3 result)
    {
        result = default;
        if (center == null) return false;

        const int attempts = 30;
        Vector3 origin = center.position;

        float minSqr = minDistance * minDistance;
        float max = Mathf.Max(minDistance, maxDistance);

        const float sampleRadius = 4f; // IMPORTANT: small snap radius

        for (int i = 0; i < attempts; i++)
        {
            Vector2 r = Random.insideUnitCircle.normalized * Random.Range(minDistance, max);
            Vector3 candidate = origin + new Vector3(r.x, 0f, r.y);

            if ((candidate - origin).sqrMagnitude < minSqr)
                continue;

            // keep above sea level
            if (candidate.y < seaLevel) candidate.y = seaLevel;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
            {
                if (hit.position.y < seaLevel) continue;
                result = hit.position;
                return true;
            }
        }

        return false;
    }

    private GameObject PickSkeletonSafe()
    {
        if (Enemies == null || Enemies.Length == 0) return null;

        // clamp indices so you don't crash if array is short
        GameObject basic = Enemies.Length > 0 ? Enemies[0] : null;
        GameObject fire = Enemies.Length > 1 ? Enemies[1] : null;
        GameObject water = Enemies.Length > 2 ? Enemies[2] : null;
        GameObject gun = Enemies.Length > 3 ? Enemies[3] : null;

        float total = basicSkeletonSpawnChance + fireSkeletonSpawnChance + waterSkeletonSpawnChance + gunEnemySpawnChance;
        if (total <= 0.0001f) return basic;

        float roll = Random.Range(0f, total);

        if (gun != null && roll < gunEnemySpawnChance) return gun;
        roll -= gunEnemySpawnChance;

        if (water != null && roll < waterSkeletonSpawnChance) return water;
        roll -= waterSkeletonSpawnChance;

        if (fire != null && roll < fireSkeletonSpawnChance) return fire;

        return basic;
    }

}
