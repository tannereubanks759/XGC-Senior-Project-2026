using UnityEngine;

public class burnUpgradeManager : MonoBehaviour
{
    public float timeOnFire =3f;
    public float damagePerTick=5f;
    private float baseTimeOnFire;
    private float baseDamagePerTick;
    private bool baseCached = false;

    void Awake()
    {
        if (!baseCached)
        {
            baseTimeOnFire = timeOnFire;
            baseDamagePerTick = damagePerTick;
            baseCached = true;
        }
    }

    public void ResetToBase()
    {
        if (!baseCached) return;
        timeOnFire = baseTimeOnFire;
        damagePerTick = baseDamagePerTick;
    }
    public void upgradeBurnTime()
    {
        timeOnFire += 2f;
    }
    public void decreaseBurnTime()
    {
        timeOnFire -= 2f;
    }
    public void upgradeBurnDamage()
    {
        damagePerTick += 1.5f;
    }
    public void decreaseBurnDamage()
    {
        damagePerTick -= 1.5f;
    }
}
