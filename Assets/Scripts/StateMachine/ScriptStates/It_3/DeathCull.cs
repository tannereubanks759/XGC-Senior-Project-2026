using UnityEngine;

public class DeathCull : MonoBehaviour
{
    float deathTime;
    bool canCull;
    BasicSkeleton[] enemies;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        deathTime = Time.time;
        canCull = false;
        enemies = GameObject.FindObjectsByType<BasicSkeleton>(sortMode: FindObjectsSortMode.None);
    }

    // Update is called once per frame
    void Update()
    {
        canCull = (Time.time - deathTime >= 15);
    }

    private void OnBecameInvisible()
    {
        if (canCull) Destroy(this);
    }
}
