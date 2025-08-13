using NUnit.Framework;
using UnityEngine;

public class WeaponsManager : MonoBehaviour
{
    public KeyCode SwordKey = KeyCode.Alpha1;
    public KeyCode GunKey = KeyCode.Alpha2;
    public KeyCode changeWeapon = KeyCode.WheelUp;
    public KeyCode changeWeaponAlt = KeyCode.WheelDown;
    public GameObject currentWeapon;
    public GameObject weapons;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
