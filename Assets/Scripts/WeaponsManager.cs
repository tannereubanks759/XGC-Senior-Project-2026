using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System;
public class WeaponsManager : MonoBehaviour
{
    public KeyCode SwordKey = KeyCode.Alpha1;
    public KeyCode GunKey = KeyCode.Alpha2;
    public int currentWeapon = 0;
    public GameObject[] weapons;
    private GameObject[] weaponIcons;
    public GameObject lamp;
    public GameObject healthPotion;
    public bool healing = false;
    public bool isPaused;
    public GameObject explosionFire;
    public GameObject invulnerabilityFire;
    public Transform MainCameraTransform;

    [Header("Sounds")]
    public AudioSource soundSource;


    private offhandHandler offhandHandle;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isPaused = false;
        weaponIcons = GameObject.FindGameObjectsWithTag("WeaponIcon");
        //Array.Reverse(weaponIcons);
        InitializeWeapons();
        if (explosionFire)
        {
            explosionFire.SetActive(false);
        }
        if (invulnerabilityFire)
        {
            invulnerabilityFire.SetActive(false);
        }
        offhandHandle = GameObject.FindAnyObjectByType<offhandHandler>();
        lamp.SetActive(false);
        
    }
    //
    // Update is called once per frame
    void Update()
    {
        if (isPaused) return;

        if(healing == false && MainCameraTransform.transform.position.y >= 0f)
        {
            if (weapons[currentWeapon].activeSelf == false)
            {
                weapons[currentWeapon].SetActive(true);
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0)
            { //mouse wheel up
                if (currentWeapon != weapons.Length - 1)
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
                if (currentWeapon != 0)
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


            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                swapLantern();      
            }
        }
        else if(MainCameraTransform.position.y < 0f && weapons[currentWeapon].activeSelf == true)
        {
            weapons[currentWeapon].SetActive(false);
            offhandHandle.unequip_Safe();
        }
    }
    public void swapLantern()
    {
        if (lamp.activeSelf)
        {
            explosionFire.SetActive(false);
            lamp.SetActive(false);
        }
        else
        {
            explosionFire.SetActive(true);
            lamp.SetActive(true);
        }
    }
    public void ShowExplosionLantern()
    {
        lamp.SetActive(true);
        explosionFire.SetActive(true);
        invulnerabilityFire.SetActive(false);
    }

    public void ShowInvulnerabilityLantern()
    {
        lamp.SetActive(true);
        invulnerabilityFire.SetActive(true);
        explosionFire.SetActive(false);
    }

    public void HideLantern()
    {
        lamp.SetActive(false);
        explosionFire.SetActive(false);
        invulnerabilityFire.SetActive(false);
    }
    public void SetHealing(bool heal)
    {
        
        if(heal == true)
        {
            weapons[currentWeapon].SetActive(false);
            healthPotion.SetActive(true);
            healing = true;
        }
        else
        {
            weapons[currentWeapon].SetActive(true);
            healthPotion.SetActive(false);
            healing = false;
        }
    }
    public void invulnerabilitySwap()
    {
       
        
        if (lamp.activeSelf)
        {
            invulnerabilityFire.SetActive(false);
            lamp.SetActive(false);
        }
        else
        {
            invulnerabilityFire.SetActive(true);
            explosionFire.SetActive(false);
            lamp.SetActive(true);
        }

    }
    void InitializeWeapons()
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (i == currentWeapon)
            {
                weapons[i].SetActive(true);
                /*if(weaponIcons.Length > 0)
                {
                    EnlargeWeaponIcon(weaponIcons[i].GetComponent<RawImage>()); 
                }*/
            }
            else
            {
                weapons[i].SetActive(false);
            }
        }
    }

    public void SwitchWeapon(int weaponSlot)
    {
        weapons[currentWeapon].SetActive(false);
        currentWeapon = weaponSlot;
        weapons[weaponSlot].SetActive(true);
        if (weapons[weaponSlot].GetComponent<Blunderbuss>())
        {
            offhandHandle.unequip_Safe();
        }
        /*if (weaponIcons.Length > 0)
        {
            EnlargeWeaponIcon(weaponIcons[currentWeapon].GetComponent<RawImage>());
        }*/
        /*if(currentWeapon == 0)
        {
            EnableCrosshair(false);
        }
        else
        {
            EnableCrosshair(true);
        }*/
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
