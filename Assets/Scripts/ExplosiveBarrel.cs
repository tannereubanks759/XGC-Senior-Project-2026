using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosiveBarrel : MonoBehaviour
{
    [Header("Explosion Radius")]
    public float instantKillRadius = 3f;
    public float damageRadius = 6f;

    [Header("References")]
    public Collider col;                  // Existing box collider. Removed when explosion happens.
    public GameObject explosionPrefab;    // FX instantiated when explosion happens.
    public MeshRenderer render;           // Turn off renderer when explosion happens.

    [Header("Damage")]
    public float dmgToEnemiesMax = 40f;
    public float dmgToPlayerMax = 100f;   // Damage at edge of instantKillRadius. Inside instantKillRadius = 999.

    [Header("Expansion")]
    public float explosionExpandTime = 0.15f; // How fast the trigger sphere grows.

    private SphereCollider explosionTrigger;
    private bool hasExploded = false;

    // Prevent damaging the same object multiple times while the trigger expands.
    private readonly HashSet<GameObject> damagedObjects = new HashSet<GameObject>();

    void Start()
    {
        if (col == null)
            col = GetComponent<Collider>();

        if (render == null)
            render = GetComponent<MeshRenderer>();
    }

    void Update()
    {
    }

    public void Explode()
    {
        if (hasExploded)
            return;

        hasExploded = true;

        if (explosionPrefab != null)
            Destroy(Instantiate(explosionPrefab, transform.position, Quaternion.identity), 3f);

        if (render != null)
            render.enabled = false;

        if (col != null)
            Destroy(col);

        explosionTrigger = gameObject.AddComponent<SphereCollider>();
        explosionTrigger.isTrigger = true;
        explosionTrigger.radius = 0f;
        explosionTrigger.center = Vector3.zero;

        StartCoroutine(ExpandExplosionTrigger());
    }

    private IEnumerator ExpandExplosionTrigger()
    {
        float elapsed = 0f;

        while (elapsed < explosionExpandTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / explosionExpandTime);

            if (explosionTrigger != null)
                explosionTrigger.radius = Mathf.Lerp(0f, damageRadius, t);

            yield return null;
        }

        if (explosionTrigger != null)
            explosionTrigger.radius = damageRadius;

        yield return new WaitForSeconds(0.1f);

        if (explosionTrigger != null)
            Destroy(explosionTrigger);

        Destroy(gameObject);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!hasExploded)
            return;

        if (other == null)
            return;

        GameObject targetRoot = other.transform.root.gameObject;

        if (damagedObjects.Contains(targetRoot))
            return;

        float distance = Vector3.Distance(transform.position, other.ClosestPoint(transform.position));

        if (distance > damageRadius)
            return;

        CombatController player = targetRoot.GetComponentInChildren<CombatController>();
        if (player != null)
        {
            damagedObjects.Add(targetRoot);
            float damage = CalculatePlayerDamage(distance);
            player.TakeDamage((int)damage);
            return;
        }

        DamageRef enemy = targetRoot.GetComponentInChildren<DamageRef>();
        if (enemy != null)
        {
            damagedObjects.Add(targetRoot);
            float damage = CalculateEnemyDamage(distance);
            enemy.TakeDamage(damage);
        }
    }

    private float CalculatePlayerDamage(float distance)
    {
        if (distance <= instantKillRadius)
            return 999f;

        if (distance >= damageRadius)
            return 0f;

        float t = Mathf.InverseLerp(instantKillRadius, damageRadius, distance);
        return Mathf.Lerp(dmgToPlayerMax, 0f, t);
    }

    private float CalculateEnemyDamage(float distance)
    {
        if (distance >= damageRadius)
            return 0f;

        float t = Mathf.InverseLerp(0f, damageRadius, distance);
        return Mathf.Lerp(dmgToEnemiesMax, 0f, t);
    }
}