using UnityEngine;

public class FireballManager : MonoBehaviour
{
    public GameObject parent;
    public KeyCode useKey = KeyCode.F;
    public GameObject FireballPref;
    public GameObject fireball_1;
    public GameObject fireball_2;
    public int activeFireballs = 2;
    public bool subRadiusUpgrade = false;
    public bool subDamageUpgrade = false;  
    public upgradeTracker upgradeTracker;
    [Header("Charging / Scale")]
    public float totalChargeTime = 8f;                 // seconds from 0 → 100%
    [Range(0f, 1f)] public float scaleAtPopStart = 0.6f;       // 60% size before pop
    [Range(0f, 1f)] public float popStartTimeFraction = 0.75f; // pop in last 25% of time

    [Header("Particle Speed")]
    public float chargingSimSpeed = 0.35f;   // while < 60% scale
    public float fullSimSpeed = 1f;          // at 100% scale

    [Header("Sounds")]
    public AudioSource soundSource;
    public AudioClip flameShoot;
    public AudioClip flameCharge;

    private Vector3 targetScale;

    private bool fireball_1_active = true;
    private bool fireball_2_active = true;

    // timers for the recharge of each fireball
    private float fireball1ChargeTimer = 0f;
    private float fireball2ChargeTimer = 0f;

    public bool upgradeOne = false; //"Increase Splash Range"
    public bool upgradeTwo = false; //"Set Enemies On Fire For A Few Seconds"

    // Particle systems for the held fireballs
    private ParticleSystem ps1;
    private ParticleSystem ps2;

    // Renderers + base colors so we can lerp from black -> original
    private Renderer rend1;
    private Renderer rend2;
    private Color baseColor1;
    private Color baseColor2;

    void Start()
    {
        parent.SetActive(false);
        targetScale = fireball_1.transform.localScale; // assumed final/full scale

        // Grab particle systems
        ps1 = fireball_1.GetComponentInChildren<ParticleSystem>();
        ps2 = fireball_2.GetComponentInChildren<ParticleSystem>();

        // Grab renderers for color control
        rend1 = fireball_1.GetComponentInChildren<Renderer>();
        rend2 = fireball_2.GetComponentInChildren<Renderer>();

        if (rend1 != null)
            baseColor1 = rend1.material.color;
        if (rend2 != null)
            baseColor2 = rend2.material.color;

        // Start them as fully powered visually
        SetParticleSimSpeed(ps1, fullSimSpeed);
        SetParticleSimSpeed(ps2, fullSimSpeed);
    }

    void Update()
    {
        // --- ONLY ONE CAN CHARGE AT A TIME, PRIORITIZE WHICHEVER STARTED FIRST ---

        bool fb1NeedsCharge = !fireball_1_active;
        bool fb2NeedsCharge = !fireball_2_active;

        if (fb1NeedsCharge && fb2NeedsCharge)
        {
            // Both are empty. Prioritize the one that has already started charging
            // (higher timer). If equal, fireball_1 wins the tie.
            if (fireball1ChargeTimer >= fireball2ChargeTimer)
            {
                RechargeFireball(fireball_1, ps1, rend1, baseColor1, ref fireball_1_active, ref fireball1ChargeTimer);
            }
            else
            {
                RechargeFireball(fireball_2, ps2, rend2, baseColor2, ref fireball_2_active, ref fireball2ChargeTimer);
            }
        }
        else if (fb1NeedsCharge)
        {
            RechargeFireball(fireball_1, ps1, rend1, baseColor1, ref fireball_1_active, ref fireball1ChargeTimer);
        }
        else if (fb2NeedsCharge)
        {
            RechargeFireball(fireball_2, ps2, rend2, baseColor2, ref fireball_2_active, ref fireball2ChargeTimer);
        }

        // ------------------------------------------------------------------------------

        

        if (!parent.activeSelf) return; //wont work if fireballs disabled past this point

        if (Input.GetKeyDown(useKey) && activeFireballs > 0)
        {
            Throw();
        }
    }

    /// <summary>
    /// Charges a fireball from 0 → 100% over totalChargeTime.
    /// - First part: grows up to ~60% linearly.
    /// - Last part: pops from 60% → 100% with a cubic (exponential-feeling) curve.
    /// Also:
    /// - Particle sim speed: 0.35 while < 60%, ramps to 1.0 by 100%.
    /// - Color: black at 0, original color at 100%, lerped as it scales.
    /// </summary>
    void RechargeFireball(
        GameObject fb,
        ParticleSystem ps,
        Renderer rend,
        Color baseColor,
        ref bool isActive,
        ref float chargeTimer)
    {
        // we only call this when !isActive, so no need to early-return

        chargeTimer += Time.deltaTime;
        float t = Mathf.Clamp01(chargeTimer / totalChargeTime); // normalized 0–1 over time

        float scale01;

        if (t <= popStartTimeFraction)
        {
            // linear 0 → scaleAtPopStart over the first portion of time
            float linT = t / popStartTimeFraction; // remap [0, popStartTimeFraction] to [0,1]
            scale01 = Mathf.Lerp(0f, scaleAtPopStart, linT);
        }
        else
        {
            // pop phase: scaleAtPopStart → 1 with a cubic ease (feels exponential)
            float u = (t - popStartTimeFraction) / (1f - popStartTimeFraction); // [0,1]
            float eased = u * u * u; // cubic curve
            scale01 = Mathf.Lerp(scaleAtPopStart, 1f, eased);
        }

        // Apply scale
        fb.transform.localScale = targetScale * scale01;

        // === Particle simulation speed control ===
        if (ps != null)
        {
            float simSpeed;

            if (scale01 <= scaleAtPopStart)
            {
                // Still in "low power" charge zone
                simSpeed = chargingSimSpeed;
            }
            else
            {
                // Scale01 is between scaleAtPopStart and 1, so ramp speed 0.35 -> 1
                float u = (scale01 - scaleAtPopStart) / (1f - scaleAtPopStart); // [0,1]
                float easedU = u * u; // curved ramp for extra pop
                simSpeed = Mathf.Lerp(chargingSimSpeed, fullSimSpeed, easedU);
            }

            SetParticleSimSpeed(ps, simSpeed);
        }

        // === Color control: black -> original color as it scales ===
        if (rend != null)
        {
            float colorT = Mathf.Clamp01(scale01); // 0 (black) → 1 (baseColor)
            Color c = Color.Lerp(Color.black, baseColor, colorT);
            rend.material.color = c;
        }
        // ========================================

        // when fully charged, mark as active and bump activeFireballs once
        if (t >= 1f && !isActive)
        {
            fb.transform.localScale = targetScale;
            isActive = true;
            activeFireballs++;

            // ensure visuals are at "full power" at the very end
            SetParticleSimSpeed(ps, fullSimSpeed);
            if (rend != null)
            {
                rend.material.color = baseColor;
            }
            soundSource.PlayOneShot(flameCharge, 2f);
        }
    }

    void SetParticleSimSpeed(ParticleSystem ps, float speed)
    {
        if (ps == null) return;
        var main = ps.main;
        main.simulationSpeed = speed;
    }

    public void UpgradeOne()
    {
        upgradeOne = true;
        upgradeTracker.fireRadiusM = true;
    }

    public void UpgradeTwo()
    {
        upgradeTwo = true;
        upgradeTracker.FireFire = true;
    }

    void Throw()
    {
        activeFireballs--;
        int random = Random.Range(0, 2);

        if (random == 0)
        {
            if (fireball_1_active)
            {
                fireball_1_active = false;
                ActivateThrow(fireball_1, ps1, rend1, ref fireball1ChargeTimer);
            }
            else
            {
                fireball_2_active = false;
                ActivateThrow(fireball_2, ps2, rend2, ref fireball2ChargeTimer);
            }
        }
        else
        {
            if (fireball_2_active)
            {
                fireball_2_active = false;
                ActivateThrow(fireball_2, ps2, rend2, ref fireball2ChargeTimer);
            }
            else
            {
                fireball_1_active = false;
                ActivateThrow(fireball_1, ps1, rend1, ref fireball1ChargeTimer);
            }
        }

        if (soundSource)
        {
            soundSource.PlayOneShot(flameShoot);
        }
    }

    void ActivateThrow(GameObject obj, ParticleSystem ps, Renderer rend, ref float chargeTimer)
    {
        GameObject fireball = Instantiate(FireballPref, obj.transform.position, Camera.main.transform.rotation);
        Fireball fb = fireball.GetComponent<Fireball>();

        if (upgradeOne)
        {
            fb.splashRadius *= 1.5f;
        }
        if (upgradeTwo)
        {
            fb.setEnemiesOnFire = true;
        }
        if(subRadiusUpgrade)
        {
            fb.splashRadius *= 1.1f;
        }
        if(subDamageUpgrade) 
        { //was 1.1f
            fb.damage *= 1.1f;
        }
        Destroy(fireball, 5f);

        // reset held orb for recharge
        obj.transform.localScale = Vector3.zero;
        chargeTimer = 0f;          // start counting 0 → 8s again for this orb

        // while recharging from 0, keep particles in slow mode and make it black
        SetParticleSimSpeed(ps, chargingSimSpeed);
        if (rend != null)
        {
            rend.material.color = Color.black;
        }
    }
    public void upgradeSplashRadius()
    {
        subRadiusUpgrade = true;
    }
    public void upgradeDamage()
    {
       subDamageUpgrade = true;
    }
}
