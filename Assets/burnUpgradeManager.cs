using UnityEngine;

public class burnUpgradeManager : MonoBehaviour
{
    public float timeOnFire =3f;
    public float damagePerTick=5f;
    public void upgradeBurnTime()
    {
        timeOnFire += 2f;
    }
    public void upgradeBurnDamage()
    {
        damagePerTick += 1.5f;
    }
}
