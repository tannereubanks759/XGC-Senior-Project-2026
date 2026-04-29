using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class levelSavingManager : MonoBehaviour
{
    public static levelSavingManager Instance;
    public Uiupdater uiupdater;
    public GoldBank gb;
    public HealthPotion healthPotion;
    public CombatController combatController;
    public Blunderbuss blunderbuss;
    [Header("Live run state")]
    public levelStartSaving current = new levelStartSaving();
    private static levelStartSaving levelStartSnapshot;
    private static levelStartSaving checkpointSnapshot;
    private static string levelStartSceneName;
    public static Vector3? checkpointRespawnPosition;
    public static Quaternion? checkpointRespawnRotation;

    public void CaptureCheckpoint()
    {
        gb = FindFirstObjectByType<GoldBank>();
        healthPotion = FindFirstObjectByType<HealthPotion>();
        combatController = FindFirstObjectByType<CombatController>();
        blunderbuss = FindFirstObjectByType<Blunderbuss>(FindObjectsInactive.Include);
        var tracker = FindFirstObjectByType<upgradeTracker>();
        var oh = FindFirstObjectByType<offhandHandler>();

        levelStartSaving snapshot = new levelStartSaving();

        if (combatController != null) snapshot.health = combatController.health;
        if (gb != null) snapshot.gold = gb.gold;
        if (healthPotion != null) snapshot.healthPotions = healthPotion.GetQuantity();
        if(blunderbuss!=null)
        {
            snapshot.ammo = blunderbuss.totalAmmo;
           
        }
        if (tracker != null)
        {
            snapshot.lightningUpgradeCount = tracker.lightningUpgradeCount;
            snapshot.curseSlow = tracker.curseSlow;
            snapshot.curseReflect = tracker.curseReflect;
            snapshot.fireRadiusM = tracker.fireRadiusM;
            snapshot.FireFire = tracker.FireFire;
            snapshot.fireSide1_1 = tracker.fireSide1_1;
            snapshot.fireSide1_2 = tracker.fireSide1_2;
            snapshot.fireSide2_1 = tracker.fireSide2_1;
            snapshot.fireSide2_2 = tracker.fireSide2_2;
            snapshot.lightningSide1_1 = tracker.lightningSide1_1;
            snapshot.lightningSide1_2 = tracker.lightningSide1_2;
            snapshot.lightningSide2_1 = tracker.lightningSide2_1;
            snapshot.lightningSide2_2 = tracker.lightningSide2_2;
            snapshot.curseSide1_1 = tracker.curseSide1_1;
            snapshot.curseSide1_2 = tracker.curseSide1_2;
            snapshot.curseSide2_1 = tracker.curseSide2_1;
            snapshot.curseSide2_2 = tracker.curseSide2_2;
        }
        if (oh != null) snapshot.lightningUpgradeCount = oh.lightningUpgradeCount;

        checkpointSnapshot = snapshot;

        GameObject marker = GameObject.FindWithTag("checkpoint");
        if (marker != null)
        {
            checkpointRespawnPosition = marker.transform.position;
            checkpointRespawnRotation = marker.transform.rotation;
        }
    }
    public void ClearCheckpoint()
    {
        checkpointSnapshot = null;
        checkpointRespawnPosition = null;
        checkpointRespawnRotation = null;
    }
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

    public void RestoreToCheckpoint()
    {
        if (checkpointSnapshot == null)
        {
            RestoreToLevelStart();
            return;
        }

        current = checkpointSnapshot.Clone();
        gb = FindFirstObjectByType<GoldBank>();
        healthPotion = FindFirstObjectByType<HealthPotion>();
        combatController = FindFirstObjectByType<CombatController>();
        blunderbuss = FindFirstObjectByType<Blunderbuss>(FindObjectsInactive.Include);
        if (gb != null) { gb.gold = current.gold; gb.UpdateGold(); }
        if (healthPotion != null)
        {
            healthPotion.SetQuantity(current.healthPotions);
            healthPotion.SetText(current.healthPotions.ToString());
        }
        if (combatController != null) combatController.health = current.health;
        if (blunderbuss != null)
        {
           
            blunderbuss.totalAmmo = current.ammo;
            blunderbuss.SetLoaded();
        }
        var tracker = FindFirstObjectByType<upgradeTracker>();
        if (tracker != null)
        {
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
            tracker.ReapplyUpgrades();
        }

        var oh = FindFirstObjectByType<offhandHandler>();
        if (oh != null) { oh.lightningUpgradeCount = current.lightningUpgradeCount; oh.lightning(false); }

        uiupdater.updateUI();
    }

    public bool HasCheckpoint()
    {
        return checkpointSnapshot != null;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        combatController = FindFirstObjectByType<CombatController>();
        gb = FindFirstObjectByType<GoldBank>();
        healthPotion = FindFirstObjectByType<HealthPotion>();
        var tracker = FindFirstObjectByType<upgradeTracker>();
        var oh = FindFirstObjectByType<offhandHandler>();


        if (checkpointRespawnPosition.HasValue)
        {
            StartCoroutine(RestoreCheckpointNextFrame());
        }
        else
        {
            if (levelStartSceneName != scene.name)
            {
                if (combatController != null) current.health = combatController.health;
                if (gb != null) current.gold = gb.gold;
                if (healthPotion != null) current.healthPotions = healthPotion.GetQuantity();
                var bb = FindFirstObjectByType<Blunderbuss>(FindObjectsInactive.Include);
                if (bb != null) current.ammo = bb.totalAmmo;
                if (tracker != null)
                {
                    current.lightningUpgradeCount = tracker.lightningUpgradeCount;
                    current.curseSlow = tracker.curseSlow;
                    current.curseReflect = tracker.curseReflect;
                    current.fireRadiusM = tracker.fireRadiusM;
                    current.FireFire = tracker.FireFire;
                    current.fireSide1_1 = tracker.fireSide1_1;
                    current.fireSide1_2 = tracker.fireSide1_2;
                    current.fireSide2_1 = tracker.fireSide2_1;
                    current.fireSide2_2 = tracker.fireSide2_2;
                    current.lightningSide1_1 = tracker.lightningSide1_1;
                    current.lightningSide1_2 = tracker.lightningSide1_2;
                    current.lightningSide2_1 = tracker.lightningSide2_1;
                    current.lightningSide2_2 = tracker.lightningSide2_2;
                    current.curseSide1_1 = tracker.curseSide1_1;
                    current.curseSide1_2 = tracker.curseSide1_2;
                    current.curseSide2_1 = tracker.curseSide2_1;
                    current.curseSide2_2 = tracker.curseSide2_2;
                }
                if (oh != null) current.lightningUpgradeCount = oh.lightningUpgradeCount;

                CaptureLevelStart();
                levelStartSceneName = scene.name;
            }

            RestoreToLevelStart();
        }
    }
    private IEnumerator RestoreCheckpointNextFrame()
{
    yield return null;
    RestoreToCheckpoint();

    var player = FindFirstObjectByType<FirstPersonController>();
    if (player != null)
    {
        CharacterController cc = player.GetComponentInChildren<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.transform.position = checkpointRespawnPosition.Value;
        player.transform.rotation = checkpointRespawnRotation.Value;
        if (cc != null) cc.enabled = true;
    }
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
        blunderbuss = FindFirstObjectByType<Blunderbuss>(FindObjectsInactive.Include);
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
        if(blunderbuss != null)
        {
            blunderbuss.totalAmmo = current.ammo;
            blunderbuss.SetLoaded();
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
