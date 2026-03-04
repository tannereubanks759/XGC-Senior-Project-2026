using UnityEngine;
using UnityEngine.SceneManagement;


public class levelSavingManager : MonoBehaviour
{
    public static levelSavingManager Instance;
    public Uiupdater uiupdater;
    public GoldBank gb;
    public HealthPotion healthPotion;
    public CombatController combatController;
    [Header("Live run state")]
    public levelStartSaving current = new levelStartSaving();

    private levelStartSaving levelStartSnapshot;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        combatController = FindFirstObjectByType<CombatController>();
        current.health = combatController.health;
        //potions and gold
        gb = FindFirstObjectByType<GoldBank>();
        healthPotion = FindFirstObjectByType<HealthPotion>();
        if (gb == null || healthPotion == null) return;
        current.gold = gb.gold;
        current.healthPotions = healthPotion.GetQuantity();
        //upgrades
        var tracker = FindFirstObjectByType<upgradeTracker>();
        if (tracker != null)
        {
            //current.lightningKnockBack = tracker.lightningKnockBack;
            //current.lightningExplosion = tracker.lightningExplosion;
            current.lightningUpgradeCount = tracker.lightningUpgradeCount;
            current.curseSlow = tracker.curseSlow;
            current.curseReflect = tracker.curseReflect;
            current.fireRadiusM = tracker.fireRadiusM;
            current.FireFire = tracker.FireFire;
            //fire sides
            current.fireSide1_1 = tracker.fireSide1_1;
            current.fireSide1_2 = tracker.fireSide1_2;
            current.fireSide2_1 = tracker.fireSide2_1;
            current.fireSide2_2 = tracker.fireSide2_2;
            //light sides
            current.lightningSide1_1 = tracker.lightningSide1_1;
            current.lightningSide1_2 = tracker.lightningSide1_2;
            current.lightningSide2_1 = tracker.lightningSide2_1;
            current.lightningSide2_2 = tracker.lightningSide2_2;
            //curse sides
            current.curseSide1_1 = tracker.curseSide1_1;
            current.curseSide1_2 = tracker.curseSide1_2;
            current.curseSide2_1 = tracker.curseSide2_1;
            current.curseSide2_2 = tracker.curseSide2_2;
        }
        var oh = FindFirstObjectByType<offhandHandler>();
        if (oh != null)
        {
            current.lightningUpgradeCount = oh.lightningUpgradeCount;
        }
        CaptureLevelStart();

        CaptureLevelStart();
    }

    public void CaptureLevelStart()
    {
        levelStartSnapshot = current.Clone();
    }

    public void RestoreToLevelStart()
    {
        if (levelStartSnapshot == null)
        {
            return;
        }
        //health potions and gold
        current = levelStartSnapshot.Clone();
        gb = FindFirstObjectByType<GoldBank>();
        healthPotion = FindFirstObjectByType<HealthPotion>();
        combatController = FindFirstObjectByType<CombatController>();
        if (gb != null)
        {
            gb.gold = current.gold;
            gb.UpdateGold();
        }
        if (healthPotion != null)
        {
            healthPotion.SetQuantity(current.healthPotions);
            healthPotion.SetText(current.healthPotions.ToString());
        }
        if(combatController != null)
        {
            combatController.health = current.health;
        }
        // upgrades
        var tracker = FindFirstObjectByType<upgradeTracker>();
        if (tracker != null)
        {
            //tracker.lightningKnockBack = current.lightningKnockBack;
            //tracker.lightningExplosion = current.lightningExplosion;
            tracker.lightningUpgradeCount = current.lightningUpgradeCount;
            tracker.curseSlow = current.curseSlow;
            tracker.curseReflect = current.curseReflect;
            tracker.fireRadiusM = current.fireRadiusM;
            tracker.FireFire = current.FireFire;
            tracker.fireSide1_1 = current.fireSide1_1;
            tracker.fireSide1_2 = current.fireSide1_2;
            tracker.fireSide2_1 = current.fireSide2_1;
            tracker.fireSide2_2 = current.fireSide2_2;
            tracker.lightningSide1_1 = current.lightningSide1_1;
            tracker.lightningSide1_2 = current.lightningSide1_2;
            tracker.lightningSide2_1 = current.lightningSide2_1;
            tracker.lightningSide2_2 = current.lightningSide2_2;
            tracker.curseSide1_1 = current.curseSide1_1;
            tracker.curseSide1_2 = current.curseSide1_2;
            tracker.curseSide2_1 = current.curseSide2_1;
            tracker.curseSide2_2 = current.curseSide2_2;
        }
        var oh = FindFirstObjectByType<offhandHandler>();
        if (oh != null)
        {
            oh.lightningUpgradeCount = current.lightningUpgradeCount;
            oh.lightning(false);
        }

        if (tracker != null)
        {
            tracker.ReapplyUpgrades();
        }
        uiupdater.updateUI();

    }

}
