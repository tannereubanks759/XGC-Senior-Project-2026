// Put this in SpawnedBySpawner.cs
using UnityEngine;

public sealed class SpawnedBySpawner : MonoBehaviour
{
    [HideInInspector] public string spawnerId; // who created me
}
