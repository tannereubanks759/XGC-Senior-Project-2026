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
    public int explosionDamage = 40;
    
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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        WeaponsManager = GetComponentInChildren<WeaponsManager>();
        UpdateSphereColors();
    }
    public void explode()
    {
        //Debug.Log("BOOM");
        //KNOCKBACK LOGIC
        // spawn particle effect and knockback
        Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);

        // apply damage to those in radius
        Collider[] closeEnemies = Physics.OverlapSphere(transform.position, damageRadius, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider col in closeEnemies)
        {
            Debug.Log(col);
            if (col.CompareTag("Enemy") && col.transform != this.transform)
            {
                var enemyTestScript = col.GetComponent<BasicSkeleton>();
                if (enemyTestScript.currentHealth > 0)
                {
                    enemyTestScript.TakeDamage(explosionDamage);
                }

            }

        }
        // inumerate damage over time to those enemies that got damaged
        hitCount = 0;
        UpdateSphereColors();
    }
    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.K))
        {
            explode();
        }
    }
   
    public void activate()
    {
        WeaponsManager.swapLantern();
        isActive = true;
        UpdateSphereColors();
    }
    public void deactivate()
    {
        WeaponsManager.swapLantern();
        isActive = false;
        UpdateSphereColors();
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
        UpdateSphereColors();
        if (hitCount > hitsToCharge)
        {
            explode();
        }
            
    }


}
