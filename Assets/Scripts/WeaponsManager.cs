using NUnit.Framework;
using UnityEngine;

public class WeaponsManager : MonoBehaviour
{
    public KeyCode SwordKey = KeyCode.Alpha1;
    public KeyCode GunKey = KeyCode.Alpha2;
    public KeyCode changeWeapon = KeyCode.WheelUp;
    public KeyCode changeWeaponAlt = KeyCode.WheelDown;
    private int currentWeapon = 0;
    public GameObject[] weapons;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void InitializeWeapons()
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (i == currentWeapon)
            {
                weapons[i].SetActive(true);
            }
            else
            {
                weapons[i].SetActive(false);
            }
        }
    }
}
