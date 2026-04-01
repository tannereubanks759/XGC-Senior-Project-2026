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
    public upgradeTracker upgradeTracker;
    private float baseCurseDuration;
    private float baseCurseReflectPercent;
    private float baseSlowSpeedMultiplier;
    private bool curseBaseSaved = false;
    private float baseChainDamageMultiplier;
    private float baseChainRadius;
    private float baseKnockbackForceMultiplier;
    private int baseKnockbackDamage;
    private bool lightningBaseCached = false;
    public LightningDashAbility lda;
    void Awake()
    {
        if (curseRef != null && !curseBaseSaved)
        {
            baseCurseDuration = curseRef.curseDuration;
            baseCurseReflectPercent = curseRef.curseReflectPercentL;
            baseSlowSpeedMultiplier = curseRef.slowSpeedMultiplier;
            curseBaseSaved = true;
        }
        if (!lightningBaseCached)
        {
            if (playerSwordScript != null)
            {
                baseChainDamageMultiplier = playerSwordScript.chainDamageMultiplier;
                baseChainRadius = playerSwordScript.chainRadius;
            }
            if (knockbackRef != null)
            {
                baseKnockbackForceMultiplier = knockbackRef.upgradedForceMultiplier;
                baseKnockbackDamage = knockbackRef.knockBackDamage;
            }
            lightningBaseCached = true;
        }
    }
    public void ResetCurseToBase()
    {
        if (!curseBaseSaved || curseRef == null) return;
        curseRef.curseDuration = baseCurseDuration;
        curseRef.curseReflectPercentL = baseCurseReflectPercent;
        curseRef.slowSpeedMultiplier = baseSlowSpeedMultiplier;
    }
    public void ResetLightningToBase()
    {
        if (!lightningBaseCached) return;

        if (playerSwordScript != null)
        {
            playerSwordScript.chainDamageMultiplier = baseChainDamageMultiplier;
            playerSwordScript.chainRadius = baseChainRadius;
        }

        if (knockbackRef != null)
        {
            knockbackRef.upgradedForceMultiplier = baseKnockbackForceMultiplier;
            knockbackRef.knockBackDamage = baseKnockbackDamage;
        }
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
        upgradeTracker.fireSide1_1 = true;
    }
    public void increaseFireballBurnDamage()
    {
        //fireScriptRef.damagePerTick = 7f;
        burnUpgradeManager.upgradeBurnDamage();
        upgradeTracker.fireSide2_2 = true;
    }
    public void increaseFireballBurnTime()
    {
       // fireScriptRef.timeOnFire = 5.5f;
       burnUpgradeManager.upgradeBurnTime();
        upgradeTracker.fireSide2_1 = true;
    }
    public void increaseFireballExplosionDamage()
    {
        //fireballRef.damage = 35f;
        fireballManager.upgradeDamage();
        upgradeTracker.fireSide1_2 = true;
    }
    #endregion
    #region Electric
    public void increaseElectricShockDamage()
    {
        lda.baseDamage = lda.baseDamage + 5;
        upgradeTracker.lightningSide2_2 = true;
    }
    public void increaseElectricShockRange()
    {
        lda.cooldownSeconds = 5;
        upgradeTracker.lightningSide2_1 = true;
    }
    public void increaseKnockBackRange()
    {
        knockbackRef.upgradedForceMultiplier = 2f;
        upgradeTracker.lightningSide1_2 = true;
    }
    public void increaseKnockBackDamage()
    {
        knockbackRef.knockBackDamage = 4;
        upgradeTracker.lightningSide1_1 = true;
    }
    #endregion
    #region curse
    public void increaseCurseTimeFirst()
    {
        curseRef.curseDuration += 2f;
        upgradeTracker.curseSide1_2 = true;
    }
    public void decreaseCurseTimeFirst()
    {
        curseRef.curseDuration -= 2f;
    }
    public void increaseCurseTimeMax()
    {
        curseRef.curseDuration += 2f;
        upgradeTracker.curseSide2_2 = true;
    }
    public void decreaseCurseTimeMax()
    {
        curseRef.curseDuration -= 2f;
        
    }
    public void increasedDamageReflection()
    {
        curseRef.curseReflectPercentL += .10f;
        upgradeTracker.curseSide2_1 = true;
    }
    public void decreaseDamageReflection()
    {
        curseRef.curseReflectPercentL -= .10f;
        
    }
    public void increaseSlowEffect()
    {
        curseRef.slowSpeedMultiplier += .5f;
        upgradeTracker.curseSide1_1 = true;
    }
    public void decreaseSlowEffect()
    {
        curseRef.slowSpeedMultiplier -= .5f;
        
    }
    #endregion
}
