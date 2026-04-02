using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
public class offhandHandler : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public ItemData currentOffhand;
    public ItemData[] allOffhands;
    public int lightningUpgradeCount = 0;
    public int choasUpgradeCount = 0;
    public int defenseUpgradeCount = 0;
    public int firebombUpgradeCount = 0;
    private GameObject player;
    public GameObject fireBall;
    public GameObject curse;
    public WeaponsManager wm;
    public GameObject lightningSkull;
    public chargeOffHandLatern chl;
    public GameObject chargeText;
    public bool lightningFirst = true;
    public bool curseFirst = true;
    public bool fireballFirst = true;
    public upgradeTracker upgradeTracker;
    private bool firstTime = true;
    private bool waitingForClose = false;
    private float currentTime = 0f;
    private float delayTime = .65f;
    public GameObject curseLantern;
    private curseOffhand curseScript;
    private bool wasBlunderbussActive = false;
    private bool skipLastEquiped = false;
    public LightningDashAbility lda;
    //public GameObject lightningTut;
    //public GameObject curseTut;
    //public GameObject fireballTut;
    public enum OffhandType { None, Lightning, Chaos, Defense, FireBomb }
    public OffhandType currentOffhandType = OffhandType.None;
    public OffhandType lastOffhandType = OffhandType.None;
    public UImanager uiMan;
    public PopUpMessage pum;
    [Header("Sounds")]
    public AudioSource soundSource;
    public AudioClip flameOn;
    public AudioClip flameOff;
    public float flameVolume = 1f;
    public GameObject lightningTextOne;
    public GameObject lightningTextTwo;
    public GameObject lightningTextThree;
    void Start()
    {
        player = GameObject.FindWithTag("Player");
        //chl=FindAnyObjectByType<chargeOffHandLatern>();
        curseScript = curse.GetComponent<curseOffhand>();
        wasBlunderbussActive = (wm != null && wm.weapons[1].activeSelf);
        if (firstTime)
        {
           pum.ShowMessage("Press and hold Q to see your offhand abilities.", 5f);
           firstTime = false;
        }
        //uiMan = FindAnyObjectByType<UImanager>();
    }
    private void waitingForCloseButton()
    {
        waitingForClose = true;
        currentTime = Time.unscaledTime;
    }
    public void quickSwap()
    {
        if (lastOffhandType == OffhandType.None)
        {
            return;
        }
        else if (lastOffhandType == OffhandType.Lightning)
        {
            lightning();
        }
        else if (lastOffhandType == OffhandType.Chaos)
        {
            chaos();
        }
        else if (lastOffhandType == OffhandType.Defense)
        {
            Defense();
        }
        else if (lastOffhandType == OffhandType.FireBomb)
        {
            FireBomb();
        }
    }
    public void unequip()
    {
        if (fireBall.activeSelf) //Play flame off sound
        {
            soundSource.PlayOneShot(flameOff, flameVolume);
        }
        fireBall.SetActive(false);
        lightningSkull.SetActive(false);    
        curse.SetActive(false);
        curseLantern.SetActive(false);
        if (curseScript != null) curseScript.canCurse = false;
        chargeText.SetActive(false);
        currentOffhand = null;
        foreach (ItemData item in allOffhands)
        {
            if (item != null && player!= null)
                item.OnUnEquip(player);
        }
        

    }
    public void unequip_Safe()
    {
        if (currentOffhandType != OffhandType.None)
        {
            lastOffhandType = currentOffhandType;
        }
        currentOffhandType = OffhandType.None;
        fireBall.SetActive(false);
        lightningSkull.SetActive(false);    
        curseLantern.SetActive(false);
        chargeText.SetActive(false);
        if (curseScript != null)
        {
            curseScript.canCurse = false;
        }
        
        // currentOffhand = null;
        /*foreach (ItemData item in allOffhands)
        {
            if (item != null && player!= null)
                item.OnUnEquip(player);
        }*/


    }
    public void lightning(bool showTutorial = true)
    {
        CheckForBlunderbuss();
        unequip();
        if (currentOffhandType != OffhandType.None && currentOffhandType != OffhandType.Lightning)
        {
            lastOffhandType = currentOffhandType;
        }
        lightningSkull.SetActive(true);
        chargeText.SetActive(true);
        chl.offHandType = chargeOffHandLatern.OffHandTypes.explosion;
        // base lightning
        if (allOffhands.Length > 1 && allOffhands[1] != null)
        {
            allOffhands[1].OnEquip(player);
            currentOffhand = allOffhands[1];
        }
        // upgrade 1
        if (lightningUpgradeCount >= 1 && allOffhands.Length > 2 && allOffhands[2] != null)
        {
            allOffhands[2].OnEquip(player);
            upgradeTracker.lightningUpgradeCount = 1;
            lightningTextOne.SetActive(false);
            lightningTextTwo.SetActive(true);
        }
        // upgrade 2
        if (lightningUpgradeCount >= 2 && allOffhands.Length > 3 && allOffhands[3] != null)
        {
            //allOffhands[3].OnEquip(player);
            lda.dashUnlocked = true;
            lightningTextTwo.SetActive(false);
            lightningTextThree.SetActive(true);
            upgradeTracker.lightningUpgradeCount = 2;
        }
        currentOffhandType = OffhandType.Lightning;

        if (showTutorial && lightningFirst)
        {
            lightningFirst = false;
            uiMan.openLightTutorial();
            waitingForCloseButton();
        }
    }
    public void chaos()
    {
        //skipLastEquiped = true;
        CheckForBlunderbuss();
        unequip();
        if (currentOffhandType != OffhandType.None && currentOffhandType != OffhandType.Chaos)
        {
            lastOffhandType = currentOffhandType;
        }
        curse.SetActive(true);
        curseLantern.SetActive(true);
        if (curseScript != null) curseScript.canCurse = true;
        currentOffhandType = OffhandType.Chaos;
        if (curseFirst)
        {
            curseFirst = false;
            //curseTut.SetActive(true);
            uiMan.openCurseTutorial();
            waitingForCloseButton();
        }
    }
    public void Defense()
    {
        CheckForBlunderbuss();
        unequip();
        if (currentOffhandType != OffhandType.None && currentOffhandType != OffhandType.Defense)
        {
            lastOffhandType = currentOffhandType;
        }
        chl.offHandType = chargeOffHandLatern.OffHandTypes.invulnerabilty;
        if (allOffhands.Length > 0 && allOffhands[0] != null)
        {
            allOffhands[0].OnEquip(player);
            currentOffhand = allOffhands[0];
        }
        currentOffhandType = OffhandType.Defense;
    }
    public void FireBomb()
    {
        //skipLastEquiped = true;
        CheckForBlunderbuss();
        unequip();

        if (currentOffhandType != OffhandType.None && currentOffhandType != OffhandType.FireBomb)
        {
            lastOffhandType = currentOffhandType;
        }
        fireBall.SetActive(true);
        var fireballManager = fireBall.gameObject.GetComponentInParent<FireballManager>();
        fireballManager.nextTime = Time.time + fireballManager.equipCooldown;
        soundSource.PlayOneShot(flameOn,flameVolume);
        currentOffhandType = OffhandType.FireBomb;
        if (fireballFirst)
        {
            fireballFirst = false;
            //fireballTut.SetActive(true);
            uiMan.openFireballTut();
            waitingForCloseButton();
        }

    }
    public void increaseUpgradeStatus(int num)
    {
        if (num == 1)
        {
            lightningUpgradeCount++;
            lightning(false);
        }
        else if (num == 2)
        {
            firebombUpgradeCount++;
            FireBomb();
        }
        else if (num == 3)
        {
            defenseUpgradeCount++;
            Defense();
        }
        else
        {
            choasUpgradeCount++;
            chaos();
        }
    }
    // Update is called once per frame
    void Update()
    {
        bool blunderbussActive = (wm != null && wm.weapons[1].activeSelf);
        if (wasBlunderbussActive && !blunderbussActive)
        {
            if (!skipLastEquiped)
            {
                quickSwap();
            }
            skipLastEquiped = false;
        }
        wasBlunderbussActive = blunderbussActive;
        if (!waitingForClose) return;
        if (!uiMan.ModalOpen) 
        { 
            waitingForClose = false; 
            return;
        }
        if (Time.unscaledTime - currentTime < delayTime)
        {
            return;
        }
        if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            uiMan.closeTut();
            waitingForClose = false;
        }
        
    }

    void CheckForBlunderbuss()
    {
        if (wm.weapons[1].activeSelf)
        {
            skipLastEquiped = true;
            wm.SwitchWeapon(0);
        }
    }
    public void closeTut()
    {
        uiMan.closeTut();
    }
    

}
