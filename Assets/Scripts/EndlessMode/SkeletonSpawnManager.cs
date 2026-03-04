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
                NextSpawnTime = Time.time + skeletonSpawnRate;
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
        Vector3 location = PickLocation(playerPos, spawnDistanceMin, spawnDistanceMax, 0f);
        if (location == null || location == Vector3.zero) return;

        var prefab = PickSkeleton();
        if (!prefab) return;

        Instantiate(prefab, location, Quaternion.identity);

        spawnedInWave++;
    }

public Vector3 PickLocation(Transform center, float minDistance, float maxDistance, float seaLevel)
{
    if (center == null) return Vector3.positiveInfinity; // sentinel = "failed"

    const int attempts = 30;
    Vector3 origin = center.position;

    for (int i = 0; i < attempts; i++)
    {
        Vector3 randomPoint = origin + Random.insideUnitSphere * maxDistance;

        if (Vector3.Distance(origin, randomPoint) < minDistance)
            continue;

        if (randomPoint.y < seaLevel)
            randomPoint.y = seaLevel;

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
        {
            if (hit.position.y < seaLevel)
                continue;

            return hit.position;
        }
    }

        return Vector3.zero;
}

GameObject PickSkeleton()
    {
        float total = basicSkeletonSpawnChance + fireSkeletonSpawnChance + waterSkeletonSpawnChance + gunEnemySpawnChance;
        float roll = Random.Range(0f, total);

        if (roll < gunEnemySpawnChance) return Enemies[3];
        roll -= gunEnemySpawnChance;

        if (roll < waterSkeletonSpawnChance) return Enemies[2];
        roll -= waterSkeletonSpawnChance;

        if (roll < fireSkeletonSpawnChance) return Enemies[1];

        return Enemies[0];
    }

}
