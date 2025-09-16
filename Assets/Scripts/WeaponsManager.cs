using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class WeaponsManager : MonoBehaviour
{
    public KeyCode SwordKey = KeyCode.Alpha1;
    public KeyCode GunKey = KeyCode.Alpha2;
    private int currentWeapon = 0;
    public GameObject[] weapons;
    public GameObject[] weaponIcons;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        weaponIcons = GameObject.FindGameObjectsWithTag("WeaponIcon");
        InitializeWeapons();
    }

    // Update is called once per frame
    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0){ //mouse wheel up
            if(currentWeapon != weapons.Length -1)
            {
                SwitchWeapon(currentWeapon + 1);
            }
            else
            {
                SwitchWeapon(0);
            }
        }

        if (scroll < 0) //mouse wheel down
        {
            if(currentWeapon != 0)
            {
                SwitchWeapon(currentWeapon - 1);
            }
            else
            {
                SwitchWeapon(weapons.Length - 1);
            }
        }


        if (Input.GetKeyDown(SwordKey)) //pull out sword
        {
            SwitchWeapon(0);
        }
        if (Input.GetKeyDown(GunKey)) //pull out gun
        {
            SwitchWeapon(1);
        }
    }

    void InitializeWeapons()
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (i == currentWeapon)
            {
                weapons[i].SetActive(true);
                if(weaponIcons.Length > 0)
                {
                    EnlargeWeaponIcon(weaponIcons[i].GetComponent<RawImage>()); 
                }
            }
            else
            {
                weapons[i].SetActive(false);
            }
        }
    }

    void SwitchWeapon(int weaponSlot)
    {
        weapons[currentWeapon].SetActive(false);
        currentWeapon = weaponSlot;
        weapons[weaponSlot].SetActive(true);

        if (weaponIcons.Length > 0)
        {
            EnlargeWeaponIcon(weaponIcons[currentWeapon].GetComponent<RawImage>());
        }
    }


    void EnlargeWeaponIcon(RawImage icon)
    {
        icon.gameObject.transform.localScale = Vector3.one;
        for(int i = 0; i < weaponIcons.Length; i++)
        {
            if (weaponIcons[i].GetComponent<RawImage>() != icon)
            {
                ShrinkWeaponIcon(weaponIcons[i].GetComponent<RawImage>());
            }
        }
    }

    void ShrinkWeaponIcon(RawImage icon)
    {
        icon.gameObject.transform.localScale = Vector3.one / 2;
    }
}
