using System.Collections.Generic;
using UnityEngine;

public class ExplosiveBarrel : MonoBehaviour
{
    [Header("Explosion Radius")]
    [SerializeField] private float damageRadius = 6f;

    [Header("References")]
    [SerializeField] private Collider barrelCollider;
    [SerializeField] private MeshRenderer barrelRenderer;
    [SerializeField] private GameObject explosionFxChild;

    [Header("Damage")]
    [SerializeField] private float dmgToEnemiesMax = 40f;
    [SerializeField] private float dmgToPlayerMax = 100f;

    [Header("Physics")]
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private int overlapBufferSize = 32;

    [Header("FX")]
    [SerializeField] private float fxLifetime = 3f;

    private bool hasExploded;

    private Collider[] overlapResults;
    private readonly HashSet<GameObject> damagedRoots = new HashSet<GameObject>();
    public GameObject ammoBag;
    private void Awake()
    {
        if (barrelCollider == null)
            barrelCollider = GetComponent<Collider>();

        if (barrelRenderer == null)
            barrelRenderer = GetComponent<MeshRenderer>();

        overlapResults = new Collider[Mathf.Max(8, overlapBufferSize)];

        if (explosionFxChild != null)
            explosionFxChild.SetActive(false);

        ammoBag.SetActive(false);
    }

    public void Explode()
    {
        if (hasExploded)
            return;

        hasExploded = true;
        damagedRoots.Clear();

        TryDamageSelfRoot();
        DetachFromParent();
        PlayExplosionFx();
        DisableBarrelVisuals();
        ProcessExplosionHits();

        ammoBag.SetActive(true);
        ammoBag.transform.parent = null;
        ammoBag.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

        Destroy(gameObject);
    }

    private void TryDamageSelfRoot()
    {
        Transform root = transform.root;

        if (root != null && root.gameObject.layer == 9)
        {
            DamageRef selfDamageRef = root.GetComponent<DamageRef>();

            if (selfDamageRef != null)
                selfDamageRef.TakeDamage(100f);
        }
    }

    private void DetachFromParent()
    {
        if (transform.parent != null)
            transform.parent = null;
    }

    private void PlayExplosionFx()
    {
        if (explosionFxChild == null)
            return;

        explosionFxChild.transform.SetParent(null, true);
        explosionFxChild.SetActive(true);
        Destroy(explosionFxChild, fxLifetime);
        explosionFxChild = null;
    }

    private void DisableBarrelVisuals()
    {
        if (barrelRenderer != null)
            barrelRenderer.enabled = false;

        if (barrelCollider != null)
            barrelCollider.enabled = false;
    }

    private void ProcessExplosionHits()
    {
        Vector3 explosionPosition = transform.position;

        int hitCount = Physics.OverlapSphereNonAlloc(
            explosionPosition,
            damageRadius,
            overlapResults,
            hitMask,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = overlapResults[i];

            if (hitCollider == null)
                continue;

            Transform hitRootTransform = hitCollider.transform.root;
            if (hitRootTransform == null)
                continue;

            GameObject hitRoot = hitRootTransform.gameObject;

            if (!damagedRoots.Add(hitRoot))
                continue;

            float distance = Vector3.Distance(
                explosionPosition,
                hitCollider.ClosestPoint(explosionPosition)
            );

            if (distance > damageRadius)
                continue;

            if (TryDamagePlayer(hitRootTransform, distance))
                continue;

            if (TryDamageEnemy(hitRootTransform, distance))
                continue;

            TryChainExplodeBarrel(hitRootTransform);
        }

        for (int i = 0; i < hitCount; i++)
            overlapResults[i] = null;
    }

    private bool TryDamagePlayer(Transform hitRootTransform, float distance)
    {
        CombatController player = hitRootTransform.GetComponentInChildren<CombatController>();

        if (player == null)
            return false;

        float damage = CalculateFalloffDamage(dmgToPlayerMax, distance);
        player.TakeDamage(Mathf.RoundToInt(damage));
        return true;
    }

    private bool TryDamageEnemy(Transform hitRootTransform, float distance)
    {
        DamageRef enemy = hitRootTransform.GetComponentInChildren<DamageRef>();

        if (enemy == null)
            return false;

        float damage = CalculateFalloffDamage(dmgToEnemiesMax, distance);
        enemy.TakeDamage(damage);
        return true;
    }

    private void TryChainExplodeBarrel(Transform hitRootTransform)
    {
        ExplosiveBarrel otherBarrel = hitRootTransform.GetComponentInChildren<ExplosiveBarrel>();

        if (otherBarrel != null && otherBarrel != this)
            otherBarrel.Explode();
    }

    private float CalculateFalloffDamage(float maxDamage, float distance)
    {
        if (distance >= damageRadius)
            return 0f;

        float t = distance / damageRadius;
        return Mathf.Lerp(maxDamage, 0f, t);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}