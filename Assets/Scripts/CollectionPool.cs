/*
 *  CollectionPool.cs
 *  Handles the collection effect for the gold particles.
 *  When the player collects a gold particle, we will call
 *  the OnGet(); function, then we will return the effect
 *  to the pool after X amount of time. Framework was copied
 *  from the Unity documentation on object pools.
 *  
 *  By: Matthew Bolger
 */

using UnityEngine;
using UnityEngine.Pool;

public class CollectionPool : MonoBehaviour
{
    // The pool holds plain GameObjects (you can swap this for any component type).
    private IObjectPool<GameObject> pool;

    // The effect to be played
    [SerializeField] private GameObject collectionEffect;
    private Transform poolParent;

    void Awake()
    {
        if (poolParent == null)
        {
            GameObject folder = new GameObject("CollectionEffects");
            poolParent = folder.transform;
        }

        // Create a pool with the four core callbacks.
        pool = new ObjectPool<GameObject>(
            createFunc: CreateItem,
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroyItem,
            collectionCheck: true,   // helps catch double-release mistakes
            defaultCapacity: 25,
            maxSize: 50
        );
    }

    void Update()
    {
        // Press Space to spawn one pooled object for 1 second.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GameObject gameObject = pool.Get();
            gameObject.transform.position = new Vector3(0, 0, 2);

            // Return it to the pool after a short delay.
            StartCoroutine(ReturnAfter(gameObject, 1f));
        }
    }

    // Creates a new pooled GameObject the first time (and whenever the pool needs more).
    private GameObject CreateItem()
    {
        GameObject gameObject = Instantiate(collectionEffect, poolParent);
        gameObject.name = "CollectionEffect";
        gameObject.SetActive(false);
        return gameObject;
    }

    // Called when an item is taken from the pool.
    private void OnGet(GameObject gameObject)
    {
        gameObject.SetActive(true);

        var ps = gameObject.GetComponent<ParticleSystem>();
        if (ps != null) ps.Play();
        var audsor = gameObject.GetComponent<AudioSource>();
        if (audsor != null) audsor.Play();
    }

    // Called when an item is returned to the pool.
    private void OnRelease(GameObject gameObject)
    {
        gameObject.SetActive(false);
    }

    // Called when the pool decides to destroy an item (e.g., above max size).
    private void OnDestroyItem(GameObject gameObject)
    {
        Destroy(gameObject);
    }

    private System.Collections.IEnumerator ReturnAfter(GameObject gameObject, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        // Give it back to the pool.
        if (gameObject.activeInHierarchy) pool.Release(gameObject);
    }
}
