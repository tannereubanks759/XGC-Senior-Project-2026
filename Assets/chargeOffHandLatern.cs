using System.Collections;
using System.Collections.Generic;
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
    public Color inactiveColor = Color.red;
    public Color activeColor = Color.green;
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

    // Update is called once per frame
    void Update()
    {
        
    }
    public void explode()
    {
        Debug.Log("BOOM");
       //KNOCKBACK LOGIC

        hitCount = 0;
        UpdateSphereColors();
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
        if (hitCount >= hitsToCharge)
        {
            explode();
        }
            
    }


}
