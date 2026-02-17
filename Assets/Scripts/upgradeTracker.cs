using UnityEngine;

public class upgradeTracker : MonoBehaviour
{
    public sideUpgradeManager sideUpgradeManager;
    public FireballManager fireballManager;
    public burnUpgradeManager burnUpgradeManager;
    [Header("Main Upgrades")]
    public int lightningUpgradeCount;
    //public bool lightningKnockBack = false;
    //public bool lightningExplosion = false;
    public bool curseSlow = false;
    public bool curseReflect = false;
    public bool fireRadiusM = false;
    public bool FireFire = false;
    [Header("Side Upgrades")]
    public bool fireSide1_1 = false;
    public bool fireSide1_2 = false;
    public bool fireSide2_1 = false;
    public bool fireSide2_2 = false;
    public bool lightningSide1_1 = false;
    public bool lightningSide1_2 = false;
    public bool lightningSide2_1 = false;
    public bool lightningSide2_2 = false;
    public bool curseSide1_1 = false;
    public bool curseSide1_2 = false;
    public bool curseSide2_1 = false;
    public bool curseSide2_2 = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void ReapplyUpgrades()
    {
        var fire = FindFirstObjectByType<FireballManager>();
        if (fire != null)
        {
            fire.upgradeOne = fireRadiusM;
            fire.upgradeTwo = FireFire;
        }

        var curse = FindFirstObjectByType<curseOffhand>();
        if (curse != null)
        {
            curse.slowUpgrade = curseSlow;
            curse.reflectionUpgrade = curseReflect;
        }

        var offhand = FindFirstObjectByType<offhandHandler>();
        if (offhand != null)
        {
            offhand.lightningUpgradeCount = lightningUpgradeCount;
            offhand.lightning(false);
        }

        if (sideUpgradeManager != null)
        {
            sideUpgradeManager.ResetCurseToBase();

            if (curseSide1_1) sideUpgradeManager.increaseSlowEffect();
            if (curseSide1_2) sideUpgradeManager.increaseCurseTimeFirst();
            if (curseSide2_1) sideUpgradeManager.increasedDamageReflection();
            if (curseSide2_2) sideUpgradeManager.increaseCurseTimeMax();
        }
        if (sideUpgradeManager != null)
        {
            sideUpgradeManager.ResetLightningToBase();

            if (lightningSide1_1) sideUpgradeManager.increaseKnockBackDamage();
            if (lightningSide1_2) sideUpgradeManager.increaseKnockBackRange();
            if (lightningSide2_1) sideUpgradeManager.increaseElectricShockRange();
            if (lightningSide2_2) sideUpgradeManager.increaseElectricShockDamage();
        }

        if (fireballManager != null)
        {
            fireballManager.subRadiusUpgrade = fireSide1_1;
            fireballManager.subDamageUpgrade = fireSide1_2;
        }
        if (burnUpgradeManager != null)
        {
            burnUpgradeManager.ResetToBase();

            if (fireSide2_1) burnUpgradeManager.upgradeBurnTime();
            if (fireSide2_2) burnUpgradeManager.upgradeBurnDamage();
        }
    }

}
