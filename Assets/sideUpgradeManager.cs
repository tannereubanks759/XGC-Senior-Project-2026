using UnityEngine;

public class sideUpgradeManager : MonoBehaviour
{
    private Fireball fireballRef;
    private SetOnFire fireScriptRef;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    #region Fireball
    public void increaseFireballRadius()
    {
        fireballRef.splashRadius = 5f;
    }
    public void increaseFireballBurnDamage()
    {
        fireScriptRef.damagePerTick = 7f;
    }
    public void increaseFireballBurnTime()
    {
        fireScriptRef.timeOnFire = 5.5f;
    }
    public void increaseFireballExplosionDamage()
    {
        fireballRef.damage = 35f;
    }
    #endregion
    #region Electric
    public void increaseElectricShockDamage()
    {

    }
    public void increaseElectricShockRange()
    {

    }
    public void increaseKnockBackRange()
    {

    }
    public void increaseKnockBackDamage()
    {

    }
    #endregion
    #region curse
    public void increaseCurseTimeFirst()
    {

    }
    public void increaseCurseTimeMax()
    {

    }
    public void increasedDamageReflection()
    {

    }
    public void increaseSlowEffect()
    {

    }
    #endregion
}
