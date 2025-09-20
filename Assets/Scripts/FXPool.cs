using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Drop this on any GameObject in your scene (eg. under a "Pools" parent).
/// Assign it in Blunderbuss via the inspector.
public class FXPool : MonoBehaviour
{
    // Queues per prefab (keyed by prefab instanceID)
    private readonly Dictionary<int, Queue<GameObject>> _pools = new();

    // Optional: how many to prewarm when a prefab is first seen
    [SerializeField] private int defaultPrewarm = 0;

    /// Spawn an instance of 'prefab' at position/rotation, automatically returning it to the pool after 'lifetime' seconds (if > 0).
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime = 0f, Transform parent = null)
    {
        if (prefab == null) return null;

        int key = prefab.GetInstanceID();

        if (!_pools.TryGetValue(key, out var q))
        {
            q = new Queue<GameObject>();
            _pools[key] = q;

            // Prewarm (optional)
            for (int i = 0; i < defaultPrewarm; i++)
            {
                var go = CreateNew(prefab, key);
                go.SetActive(false);
                q.Enqueue(go);
            }
        }

        GameObject instance = q.Count > 0 ? q.Dequeue() : CreateNew(prefab, key);

        PrepareForUse(instance, position, rotation, parent);

        if (lifetime > 0f)
            StartCoroutine(ReturnAfter(prefab, instance, lifetime));

        return instance;
    }

    /// Manually return an instance (if you're not using timed return).
    public void Return(GameObject prefab, GameObject instance)
    {
        if (prefab == null || instance == null) return;

        int key = prefab.GetInstanceID();
        if (!_pools.TryGetValue(key, out var q))
        {
            q = new Queue<GameObject>();
            _pools[key] = q;
        }

        // Disable and reparent under the pool for tidiness
        instance.SetActive(false);
        instance.transform.SetParent(transform, false);
        q.Enqueue(instance);
    }

    private GameObject CreateNew(GameObject prefab, int key)
    {
        var go = Instantiate(prefab, transform);
        // Tag with pool key so we know where to return it even if caller forgets which prefab it came from
        var token = go.GetComponent<FXPoolToken>();
        if (!token) token = go.AddComponent<FXPoolToken>();
        token.prefabKey = key;
        token.originPool = this;
        return go;
    }

    private void PrepareForUse(GameObject go, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (parent != null)
        {
            go.transform.SetParent(parent, true);
        }
        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);

        // Reset common transient components so pooled FX look fresh
        var psList = go.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < psList.Length; i++)
        {
            var ps = psList[i];
            ps.Clear(true);
            ps.Play(true);
        }

        var trails = go.GetComponentsInChildren<TrailRenderer>(true);
        for (int i = 0; i < trails.Length; i++)
        {
            trails[i].Clear();
        }

        var animators = go.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            animators[i].Rebind();
            animators[i].Update(0f);
        }
    }

    private IEnumerator ReturnAfter(GameObject prefab, GameObject instance, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        // Instance may have been disabled/returned already by other logic; that's fine.
        Return(prefab, instance);
    }
}

/// Attached to pooled instances so we can identify their pool/prefab even if needed.
/// (Not strictly required for the above flow, but handy if you ever call ReturnByInstance)
public class FXPoolToken : MonoBehaviour
{
    [System.NonSerialized] public FXPool originPool;
    [System.NonSerialized] public int prefabKey;
}
