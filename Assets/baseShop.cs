using System.Collections.Generic;
using UnityEngine;

public class baseShop : MonoBehaviour
{
    
    [SerializeField]
    public List<ItemData> artifacts;
    public List<GameObject> miscThings;
    public List<GameObject> spawnPoints;
    // "base" for base
    //"mid" for a mid run shop
    //"boss" for the boss shop
    private int last;
    public enum ShopType { Base, Mid, Boss }
    public ShopType shopType = ShopType.Base;
    void SpawnArtifactsAndMisc(int artifactCount, int miscCount)
    {
        // ensures no repeate artifacts are spawned
        List<int> indices = new List<int>();
        for (int k = 0; k < (artifacts != null ? artifacts.Count : 0); k++) indices.Add(k);
        for (int k = 0; k < indices.Count; k++)
        {
            int swapWith = Random.Range(k, indices.Count);
            int tmp = indices[k];
            indices[k] = indices[swapWith];
            indices[swapWith] = tmp;
        }
        last = 0;
        // spawns random artifact from pool
        for (int i = 0; i < artifactCount; i++)
        {
            int idx = indices[i];
            var obj = Instantiate(artifacts[idx].prefab, spawnPoints[i].transform.position, spawnPoints[i].transform.rotation);
            var floater = obj.GetComponent<floating>();
            floater.enabled = false;
            last = i + 1;
        }
        // spawn misc 
        for (int i = last; i < last + miscCount && i < spawnPoints.Count; i++)
        {
            int ran2 = Random.Range(0, miscThings.Count);
            var prefab = miscThings[ran2];
            Instantiate(prefab, spawnPoints[i].transform.position, prefab.transform.rotation);
            
        }
}
    public void GenerateShop()
    {
        switch (shopType)
        {
            case ShopType.Base:
                Debug.Log("attempting to spawn");
                SpawnArtifactsAndMisc(artifactCount: 1, miscCount: 4);
                Debug.Log("Spawned");
                break;
            case ShopType.Mid:
                SpawnArtifactsAndMisc(artifactCount: 2, miscCount: 3);
                break;
            case ShopType.Boss:
                SpawnArtifactsAndMisc(artifactCount: 3, miscCount: 2);
                break;
        }
    }

    private void Start()
    {
        GenerateShop();
    }
}
