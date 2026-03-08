using DigitalRuby.ThunderAndLightning;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosiveBarrel : MonoBehaviour
{
    [Header("Explosion Radius")]
    public float damageRadius = 6f;

    [Header("References")]
    public Collider col;
    public GameObject explosionPrefab;
    public MeshRenderer render;

    [Header("Damage")]
    public float dmgToEnemiesMax = 40f;
    public float dmgToPlayerMax = 100f;

    [Header("Expansion")]
    public float explosionExpandTime = 0.15f;

    private SphereCollider explosionTrigger;
    private GameObject explosionTriggerObject;
    private bool hasExploded = false;

    private readonly HashSet<GameObject> damagedObjects = new HashSet<GameObject>();

    void Start()
    {
        if (col == null)
            col = GetComponent<Collider>();

        if (render == null)
            render = GetComponent<MeshRenderer>();
    }

    public void Explode()
    {
        if (hasExploded)
            return;

        hasExploded = true;

        if (transform.root.gameObject.layer == 9)
        {
            DamageRef selfDamageRef = transform.root.GetComponent<DamageRef>();
            if (selfDamageRef != null)
                selfDamageRef.TakeDamage(100);
        }

        if (transform.parent != null)
            transform.parent = null;

        if (explosionPrefab != null)
            Destroy(Instantiate(explosionPrefab, transform.position, Quaternion.identity), 3f);

        if (render != null)
            render.enabled = false;

        if (col != null)
            Destroy(col);

        CreateExplosionTriggerObject();
        StartCoroutine(ExpandExplosionTrigger());
    }

    private void CreateExplosionTriggerObject()
    {
        explosionTriggerObject = new GameObject("ExplosionTrigger");
        explosionTriggerObject.transform.position = transform.position;
        explosionTriggerObject.transform.rotation = Quaternion.identity;
        explosionTriggerObject.transform.localScale = Vector3.one;

        ExplosionTriggerForwarder forwarder = explosionTriggerObject.AddComponent<ExplosionTriggerForwarder>();
        forwarder.owner = this;

        explosionTrigger = explosionTriggerObject.AddComponent<SphereCollider>();
        explosionTrigger.isTrigger = true;
        explosionTrigger.radius = 0f;
        explosionTrigger.center = Vector3.zero;
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

        if (explosionTriggerObject != null)
            Destroy(explosionTriggerObject);

        Destroy(gameObject);
    }

    public void HandleExplosionTriggerEnter(Collider other)
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
            return;
        }

        ExplosiveBarrel otherBarrel = targetRoot.GetComponentInChildren<ExplosiveBarrel>();
        if (otherBarrel != null && otherBarrel != this)
        {
            otherBarrel.Explode();
        }
    }

    private float CalculatePlayerDamage(float distance)
    {
        if (distance >= damageRadius)
            return 0f;

        float t = Mathf.InverseLerp(0f, damageRadius, distance);
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