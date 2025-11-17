using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class chargeOffHandLatern : MonoBehaviour
{
    public WeaponsManager WeaponsManager;
    public int hitsToCharge = 10;
    public float hitWindowSeconds = 6f;
    public int hitCount = 0;
    private Coroutine decayCorutine;
    private bool isActive = false;

    public List<Renderer> chargeSpheres;
    public GameObject explosionEffectPrefab;
    public Color inactiveColor = Color.red;
    public Color activeColor = Color.green;
    public float damageRadius = 5f;
    public int explosionDamage = 20;
    public int tickDamage;
    public float invulnerabilityTime = 5f;
    public float burnTime;

    public enum OffHandTypes
    {
        explosion,
        invulnerabilty
    }
    public OffHandTypes offHandType;
    [Header("Invulnerability Upgrades")]
    public bool longerInvulnUpgrade = false;
    public float invulnDurationMultiplier = 1.5f;
    public bool invulnPersistsOnSwapUpgrade = false;

    private CombatController CombatController;
    private Coroutine invulnCoroutine;

    private void UpdateSphereColors()
    {
        int hitsPerSphere = Mathf.CeilToInt((float)hitsToCharge / chargeSpheres.Count);
        for (int i = 0; i < chargeSpheres.Count; i++)
        {
            if (chargeSpheres[i] == null) continue;
            bool filled = hitCount >= (i + 1) * hitsPerSphere;
            Color targetColor = filled ? activeColor : inactiveColor;
            chargeSpheres[i].material.color = targetColor;
        }
    }
    public void persistUpgrade()
    {
        invulnPersistsOnSwapUpgrade = true;
    }
    public void timeUpgrade() 
    {
        longerInvulnUpgrade = true;
    }
    void Start()
    {
        WeaponsManager = GetComponentInChildren<WeaponsManager>();
        CombatController = GetComponentInChildren<CombatController>();
        UpdateSphereColors();
    }

    IEnumerator invulnerableTime()
    {
        float duration = invulnerabilityTime;

        if (longerInvulnUpgrade)
        {
            duration *= invulnDurationMultiplier;
        }

        yield return new WaitForSeconds(duration);

        CombatController.invulnerability = false;
        invulnCoroutine = null;
    }

    public void invulnerable()
    {
        // consume charge
        hitCount = 0;
        UpdateSphereColors();
        if (decayCorutine != null)
        {
            StopCoroutine(decayCorutine);
            decayCorutine = null;
        }

        CombatController.invulnerability = true;
        Debug.Log("Invulnerable Active");

        if (invulnCoroutine != null)
        {
            StopCoroutine(invulnCoroutine);
        }
        invulnCoroutine = StartCoroutine(invulnerableTime());
    }

    public void explode()
    {
        // spawn particle effect and knockback
        Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);

        // apply damage to those in radius
        Collider[] closeEnemies = Physics.OverlapSphere(transform.position, damageRadius, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider col in closeEnemies)
        {
            if (col.CompareTag("Enemy") && col.transform != this.transform)
            {
                var enemyTestScript = col.GetComponent<BasicSkeleton>();
                if (enemyTestScript != null && enemyTestScript.currentHealth > 0)
                {
                    enemyTestScript.TakeDamage(explosionDamage);
                    enemyTestScript.applyBurn(1, 1f, 5);
                }
            }
        }
        hitCount = 0;
        UpdateSphereColors();
        if (decayCorutine != null)
        {
            StopCoroutine(decayCorutine);
            decayCorutine = null;
        }
    }

    void Update()
    {
        if (!isActive) return;
        if (Input.GetKeyDown(KeyCode.F) && hitCount >= hitsToCharge)
        {
            if (offHandType == OffHandTypes.explosion)
            {
                explode();
            }
            else if (offHandType == OffHandTypes.invulnerabilty)
            {
                invulnerable();
            }
        }
    }

    public void activate()
    {
        if (offHandType == OffHandTypes.invulnerabilty)
        {
            WeaponsManager.ShowInvulnerabilityLantern();
        }
        else
        {
            WeaponsManager.ShowExplosionLantern();
        }

        isActive = true;
        UpdateSphereColors();
    }

    public void deactivate()
    {
        WeaponsManager.HideLantern();
        isActive = false;
        UpdateSphereColors();
        if (!invulnPersistsOnSwapUpgrade)
        {
            if (invulnCoroutine != null)
            {
                StopCoroutine(invulnCoroutine);
                invulnCoroutine = null;
            }
            CombatController.invulnerability = false;
        }
    }

    IEnumerator HitWindowCD()
    {
        yield return new WaitForSeconds(hitWindowSeconds);
        hitCount = 0;
        UpdateSphereColors();
        decayCorutine = null;
    }

    public void hitRegistered()
    {
        if (!isActive) return;

        if (decayCorutine != null)
        {
            StopCoroutine(decayCorutine);
        }
        decayCorutine = StartCoroutine(HitWindowCD());
        hitCount++;
        if (hitCount > hitsToCharge)
        {
            hitCount = hitsToCharge;
        }
        UpdateSphereColors();
    }
}