using UnityEngine;

public class sideUpgradeManager : MonoBehaviour
{
    public Fireball fireballRef;
    public SetOnFire fireScriptRef;
    public curseOffhand curseRef;
    public DamageRef damageRef;
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
        curseRef.curseDuration += 2f;
    }
    public void increaseCurseTimeMax()
    {
        curseRef.curseDuration += 2f;
    }
    public void increasedDamageReflection()
    {
        curseRef.curseReflectPercentL += .10f;
    }
    public void increaseSlowEffect()
    {
        curseRef.slowSpeedMultiplier += .5f;
    }
    #endregion
}
