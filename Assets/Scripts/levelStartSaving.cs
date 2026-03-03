using System;
using UnityEngine;

[Serializable]
public class levelStartSaving
{
    [Header("Currencies / consumables")]
    public int gold;
    public int healthPotions;
    public int health;
    [Header("Upgrades")]
    public int lightningUpgradeCount;
    //public bool lightningKnockBack;
    //public bool lightningExplosion;
    public bool curseSlow;
    public bool curseReflect;
    public bool fireRadiusM;
    public bool FireFire;

    public bool fireSide1_1;
    public bool fireSide1_2;
    public bool fireSide2_1;
    public bool fireSide2_2;
    public bool lightningSide1_1;
    public bool lightningSide1_2;
    public bool lightningSide2_1;
    public bool lightningSide2_2;
    public bool curseSide1_1;
    public bool curseSide1_2;
    public bool curseSide2_1;
    public bool curseSide2_2;

    public levelStartSaving Clone()
    {
        return new levelStartSaving
        {
            gold = this.gold,
            healthPotions = this.healthPotions,
            curseSlow = this.curseSlow,
            curseReflect = this.curseReflect,
            health = this.health,
           // lightningKnockBack = this.lightningKnockBack,
            //lightningExplosion = this.lightningExplosion,
            lightningUpgradeCount = this.lightningUpgradeCount,
            fireRadiusM = this.fireRadiusM,
            FireFire = this.FireFire,
            fireSide1_1 = this.fireSide1_1,
            fireSide1_2 = this.fireSide1_2,
            fireSide2_1 = this.fireSide2_1,
            fireSide2_2 = this.fireSide2_2,
            lightningSide1_1 = this.lightningSide1_1,
            lightningSide1_2 = this.lightningSide1_2,
            lightningSide2_1 = this.lightningSide2_1,
            lightningSide2_2 = this.lightningSide2_2,
            curseSide1_1 = this.curseSide1_1,
            curseSide1_2 = this.curseSide1_2,
            curseSide2_1 = this.curseSide2_1,
            curseSide2_2 = this.curseSide2_2
        };
    }
}
