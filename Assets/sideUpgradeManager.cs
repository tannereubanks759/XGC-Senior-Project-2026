using UnityEngine;

public class sideUpgradeManager : MonoBehaviour
{
    //public Fireball fireballRef;
    public FireballManager fireballManager;
    //public SetOnFire fireScriptRef;
    public curseOffhand curseRef;
    //public DamageRef damageRef;
    public burnUpgradeManager burnUpgradeManager;
    public RaycastKnockback knockbackRef;
    public PlayerSwordScript playerSwordScript;
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
        //fireballRef.splashRadius = 5f;
        fireballManager.upgradeSplashRadius();
    }
    public void increaseFireballBurnDamage()
    {
        //fireScriptRef.damagePerTick = 7f;
        burnUpgradeManager.upgradeBurnDamage();
    }
    public void increaseFireballBurnTime()
    {
       // fireScriptRef.timeOnFire = 5.5f;
       burnUpgradeManager.upgradeBurnTime();
    }
    public void increaseFireballExplosionDamage()
    {
        //fireballRef.damage = 35f;
        fireballManager.upgradeDamage();
    }
    #endregion
    #region Electric
    public void increaseElectricShockDamage()
    {
        playerSwordScript.chainDamageMultiplier = .65f;
    }
    public void increaseElectricShockRange()
    {
        playerSwordScript.chainRadius = 7.5f;
    }
    public void increaseKnockBackRange()
    {
        knockbackRef.upgradedForceMultiplier = 2f;
    }
    public void increaseKnockBackDamage()
    {
        knockbackRef.knockBackDamage = 4;
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
