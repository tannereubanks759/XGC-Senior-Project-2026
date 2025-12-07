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
    [Header("Lightning Charge")]
    public chargeBaseScript chargeBase;
    public float chargePerHit = 10f;
    public float minChargeToExplode = 30f;
    public List<Renderer> chargeSpheres;
    public GameObject explosionEffectPrefab;
    public Color inactiveColor = Color.red;
    public Color activeColor = Color.green;
    public float damageRadius = 5f;
    public int explosionDamage = 20;
    public int tickDamage;
    public float invulnerabilityTime = 5f;
    public float burnTime;
    [Header("Explosion Scaling")]
    public float maxRadiusMultiplier = 2f;
    public float maxDamageMultiplier = 2f;
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
        if (chargeSpheres == null || chargeSpheres.Count == 0) return;

        if (offHandType == OffHandTypes.explosion && chargeBase != null)
        {
            float normalized = chargeBase.maxCharge > 0f? chargeBase.currentCharge / chargeBase.maxCharge: 0f;

            int litCount = Mathf.RoundToInt(normalized * chargeSpheres.Count);

            for (int i = 0; i < chargeSpheres.Count; i++)
            {
                if (chargeSpheres[i] == null) continue;
                bool filled = i < litCount;
                Color targetColor = filled ? activeColor : inactiveColor;
                chargeSpheres[i].material.color = targetColor;
            }
        }
        else
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
        chargeBase = FindAnyObjectByType<chargeBaseScript>();   
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
 
        float currentCharge = (chargeBase != null) ? chargeBase.currentCharge : 0f;
        float t = 0f;
        if (chargeBase != null && chargeBase.maxCharge > 0f)
        {
            t = Mathf.InverseLerp(minChargeToExplode, chargeBase.maxCharge, currentCharge);
        }
        float finalRadius = damageRadius * Mathf.Lerp(1f, maxRadiusMultiplier, t);
        int finalDamage = Mathf.RoundToInt(explosionDamage * Mathf.Lerp(1f, maxDamageMultiplier, t));

        Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        Collider[] closeEnemies = Physics.OverlapSphere(transform.position, finalRadius, ~0, QueryTriggerInteraction.Ignore);

        foreach (Collider col in closeEnemies)
        {
            if (col.CompareTag("Enemy") && col.transform != this.transform)
            {
                var enemyTestScript = col.GetComponent<BasicSkeleton>();
                if (enemyTestScript != null && enemyTestScript.currentHealth > 0)
                {
                    enemyTestScript.TakeDamage(finalDamage);
                    enemyTestScript.applyBurn(1, 1f, 5);
                }
            }
            else
            {
                var bossRef = col.GetComponentInParent<DamageRef>();
                if (bossRef != null)
                {
                    var pirateBoss = bossRef.GetComponentInParent<PirateBossAI>();
                    var magmaBoss = bossRef.GetComponentInParent<MagmaBossAI>();

                    if (pirateBoss != null && pirateBoss.currentHealth > 0)
                    {
                        pirateBoss.TakeDamage(finalDamage);
                    }
                    else if (magmaBoss != null && magmaBoss.currentHealth > 0)
                    {
                        magmaBoss.TakeDamage(finalDamage);
                    }
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

        if (chargeBase != null)
        {
            chargeBase.fullReset();
            UpdateSphereColors();
        }
    }

    void Update()
    {
        if (!isActive) return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (offHandType == OffHandTypes.explosion)
            {
                if (chargeBase != null && chargeBase.currentCharge >= minChargeToExplode)
                {
                    explode();
                }
            }
            else if (offHandType == OffHandTypes.invulnerabilty)
            {
                if (hitCount >= hitsToCharge)
                {
                    invulnerable();
                }
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
            //WeaponsManager.ShowExplosionLantern();
            //enable charge logic
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
        //Debug.Log("COunted hit");
        if (!isActive&&!chargeBase.isActive) return;
        if (offHandType == OffHandTypes.explosion)
        {
            chargeBase.increaseCharge(chargePerHit);
            return;
        }
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