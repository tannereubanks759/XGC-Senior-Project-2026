using UnityEngine;

public class Uiupdater : MonoBehaviour
{
    public upgradeTracker tracker;
    [Header("Lightning UI")]
    public GameObject lightM1;
    public GameObject lightM1Check;
    public GameObject lightM1X;
    public GameObject lightM2;
    public GameObject lightM2Check;
    public GameObject lightM2X;
    public GameObject lightSideTop1;
    public GameObject lightSideTop2;
    public GameObject lightSideBottom1;
    public GameObject lightSideBottom2;
    [Header("Fireball UI")]
    public GameObject fireM1;
    public GameObject fireM1Check;
    public GameObject fireM1X;
    public GameObject fireM2;
    public GameObject fireM2Check;
    public GameObject fireM2X;
    public GameObject fireSideTop1;
    public GameObject fireSideTop2;
    public GameObject fireSideBottom1;
    public GameObject fireSideBottom2;
    [Header("Curse UI")]
    public GameObject curseM1;
    public GameObject curseM1Check;
    public GameObject curseM1X;
    public GameObject curseM2;
    public GameObject curseM2Check;
    public GameObject curseM2X;
    public GameObject curseSideTop1;
    public GameObject curseSideTop2;
    public GameObject curseSideBottom1;
    public GameObject curseSideBottom2;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void updateUI()
    {
        if (tracker == null) return;

        int lightUpgrade = tracker.lightningUpgradeCount;

        lightM1.SetActive(lightUpgrade == 0);
        lightM2.SetActive(lightUpgrade < 2);
        lightM1Check.SetActive(lightUpgrade >= 1);
        lightM2Check.SetActive(lightUpgrade >= 2);
        lightM1X.SetActive(lightUpgrade == 0);
        lightM2X.SetActive(lightUpgrade <= 1);
        lightSideTop1.SetActive(!tracker.lightningSide1_1);
        lightSideTop2.SetActive(!tracker.lightningSide1_2);
        lightSideBottom1.SetActive(!tracker.lightningSide2_1);
        lightSideBottom2.SetActive(!tracker.lightningSide2_2);

        fireM1.SetActive(!tracker.fireRadiusM);
        fireM2.SetActive(!tracker.FireFire);
        fireM1Check.SetActive(tracker.fireRadiusM);
        fireM1X.SetActive(!tracker.fireRadiusM);
        fireM2Check.SetActive(tracker.FireFire);
        fireM2X.SetActive(!tracker.FireFire);
        fireSideTop1.SetActive(!tracker.fireSide1_1);
        fireSideTop2.SetActive(!tracker.fireSide1_2);
        fireSideBottom1.SetActive(!tracker.fireSide2_1);
        fireSideBottom2.SetActive(!tracker.fireSide2_2);

        curseM1.SetActive(!tracker.curseSlow);
        curseM2.SetActive(!tracker.curseReflect);
        curseM1Check.SetActive(tracker.curseSlow);
        curseM1X.SetActive(!tracker.curseSlow);
        curseM2Check.SetActive(tracker.curseReflect);
        curseM2X.SetActive(!tracker.curseReflect);
        curseSideTop1.SetActive(!tracker.curseSide1_1);
        curseSideTop2.SetActive(!tracker.curseSide1_2);
        curseSideBottom1.SetActive(!tracker.curseSide2_1);
        curseSideBottom2.SetActive(!tracker.curseSide2_2);
    }

}
