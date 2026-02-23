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
    public float totalChargeTime = 8f;                         // seconds from 0 → 100%
    [Range(0f, 1f)] public float scaleAtPopStart = 0.6f;        // before pop
    [Range(0f, 1f)] public float popStartTimeFraction = 0.75f;  // pop in last 25%

    [Header("Uncharged Visuals")]
    [Range(0f, 1f)] public float unchargedScale = 0.5f;         // 50% size while not charged
    public Color unchargedColor = new Color(0.15f, 0.15f, 0.15f, 1f); // gray/black

    [Header("Particle Speed")]
    public float chargingSimSpeed = 0.35f;
    public float fullSimSpeed = 1f;

    [Header("Start Settings")]
    [Tooltip("Time until fireballs can be shot after they have been equipped.")]
    public float equipCooldown = 1f;
    public float nextTime = 0;

    [Header("Sounds")]
    public AudioSource soundSource;
    public AudioClip flameShoot;
    public AudioClip flameCharge;

    private Vector3 targetScale;

    // Charged state (available to shoot)
    private bool fireball_1_active = true;
    private bool fireball_2_active = true;

    // Recharging state (in progress)
    private bool fireball_1_recharging = false;
    private bool fireball_2_recharging = false;

    // Timers
    private float fireball1ChargeTimer = 0f;
    private float fireball2ChargeTimer = 0f;

    public bool upgradeOne = false; // "Increase Splash Range"
    public bool upgradeTwo = false; // "Set Enemies On Fire For A Few Seconds"

    // Particle systems for the held fireballs
    private ParticleSystem ps1;
    private ParticleSystem ps2;

    // Renderers + base colors so we can lerp from uncharged -> original
    private Renderer rend1;
    private Renderer rend2;
    private Color baseColor1;
    private Color baseColor2;

    /// <summary>
    /// True when BOTH fireballs are either:
    /// - already charged, OR
    /// - currently recharging
    /// </summary>
    [Header("Read-Only State")]
    public bool bothReadyOrRecharging;

    void Start()
    {
        parent.SetActive(false);
        targetScale = fireball_1.transform.localScale; // assumed final/full scale

        ps1 = fireball_1.GetComponentInChildren<ParticleSystem>();
        ps2 = fireball_2.GetComponentInChildren<ParticleSystem>();

        rend1 = fireball_1.GetComponentInChildren<Renderer>();
        rend2 = fireball_2.GetComponentInChildren<Renderer>();

        if (rend1 != null) baseColor1 = rend1.material.color;
        if (rend2 != null) baseColor2 = rend2.material.color;

        // Start them as fully powered visually
        ApplyFullVisuals(fireball_1, ps1, rend1, baseColor1);
        ApplyFullVisuals(fireball_2, ps2, rend2, baseColor2);
    }

    void Update()
    {
        // Update the "both ready or recharging" bool
        bothReadyOrRecharging =
            (fireball_1_active || fireball_1_recharging) &&
            (fireball_2_active || fireball_2_recharging);

        // Recharge ONLY if recharging was started via CollectFire()
        if (fireball_1_recharging && !fireball_1_active)
        {
            RechargeFireball(
                fireball_1, ps1, rend1, baseColor1,
                ref fireball_1_active, ref fireball_1_recharging, ref fireball1ChargeTimer);
        }

        if (fireball_2_recharging && !fireball_2_active)
        {
            RechargeFireball(
                fireball_2, ps2, rend2, baseColor2,
                ref fireball_2_active, ref fireball_2_recharging, ref fireball2ChargeTimer);
        }

        if (!parent.activeSelf) return;
        if (Time.time < nextTime) return;

        if (Input.GetKeyDown(useKey) && activeFireballs > 0)
        {
            Throw();
        }
    }

    /// <summary>
    /// Call this from another script when the player "collects fire".
    /// Starts a recharge on ONE missing fireball (if any).
    /// Does nothing if both are already charged or already recharging.
    /// </summary>
    public void CollectFire()
    {
        // If fb1 is missing and not already recharging, start it
        if (!fireball_1_active && !fireball_1_recharging)
        {
            StartRecharge(fireball_1, ps1, rend1, ref fireball_1_recharging, ref fireball1ChargeTimer);
            return;
        }

        // Otherwise try fb2
        if (!fireball_2_active && !fireball_2_recharging)
        {
            StartRecharge(fireball_2, ps2, rend2, ref fireball_2_recharging, ref fireball2ChargeTimer);
            return;
        }

        // else: both are already charged or recharging -> do nothing
    }

    void StartRecharge(GameObject fb, ParticleSystem ps, Renderer rend, ref bool isRecharging, ref float chargeTimer)
    {
        isRecharging = true;
        chargeTimer = 0f;

        // Start at 50% size + gray/black, then RechargeFireball will grow from here
        ApplyUnchargedVisuals(fb, ps, rend);
    }

    /// <summary>
    /// Charges a fireball from "unchargedScale" → 100% over totalChargeTime.
    /// - First part: unchargedScale → scaleAtPopStart linearly.
    /// - Last part: scaleAtPopStart → 1 with a cubic ease.
    /// Also:
    /// - Particle sim speed: chargingSimSpeed while <= scaleAtPopStart, ramps to fullSimSpeed by 100%.
    /// - Color: unchargedColor at start, baseColor at end.
    /// </summary>
    void RechargeFireball(
        GameObject fb,
        ParticleSystem ps,
        Renderer rend,
        Color baseColor,
        ref bool isActive,
        ref bool isRecharging,
        ref float chargeTimer)
    {
        chargeTimer += Time.deltaTime;
        float t = Mathf.Clamp01(chargeTimer / totalChargeTime);

        float minS = Mathf.Clamp01(unchargedScale);
        float popS = Mathf.Max(scaleAtPopStart, minS); // ensure pop start isn't below 50%

        float scale01;
        if (t <= popStartTimeFraction)
        {
            float linT = (popStartTimeFraction <= 0f) ? 1f : (t / popStartTimeFraction);
            scale01 = Mathf.Lerp(minS, popS, linT);
        }
        else
        {
            float denom = Mathf.Max(0.0001f, 1f - popStartTimeFraction);
            float u = (t - popStartTimeFraction) / denom;
            float eased = u * u * u; // cubic
            scale01 = Mathf.Lerp(popS, 1f, eased);
        }

        fb.transform.localScale = targetScale * scale01;

        // Particle simulation speed
        if (ps != null)
        {
            float simSpeed;
            if (scale01 <= popS)
            {
                simSpeed = chargingSimSpeed;
            }
            else
            {
                float u = (scale01 - popS) / Mathf.Max(0.0001f, (1f - popS));
                float easedU = u * u;
                simSpeed = Mathf.Lerp(chargingSimSpeed, fullSimSpeed, easedU);
            }
            SetParticleSimSpeed(ps, simSpeed);
        }

        // Color control: unchargedColor -> baseColor
        if (rend != null)
        {
            float colorU = (scale01 - minS) / Mathf.Max(0.0001f, (1f - minS)); // 0 at 50%, 1 at full
            Color c = Color.Lerp(unchargedColor, baseColor, Mathf.Clamp01(colorU));
            rend.material.color = c;
        }

        // Done
        if (t >= 1f && !isActive)
        {
            fb.transform.localScale = targetScale;
            isActive = true;
            isRecharging = false;
            activeFireballs++;

            ApplyFullVisuals(fb, ps, rend, baseColor);

            if (soundSource && flameCharge)
                soundSource.PlayOneShot(flameCharge, 2f);
        }
    }

    void ApplyUnchargedVisuals(GameObject fb, ParticleSystem ps, Renderer rend)
    {
        if (fb != null) fb.transform.localScale = targetScale * Mathf.Clamp01(unchargedScale);
        SetParticleSimSpeed(ps, chargingSimSpeed);
        if (rend != null) rend.material.color = unchargedColor;
    }

    void ApplyFullVisuals(GameObject fb, ParticleSystem ps, Renderer rend, Color baseColor)
    {
        if (fb != null) fb.transform.localScale = targetScale;
        SetParticleSimSpeed(ps, fullSimSpeed);
        if (rend != null) rend.material.color = baseColor;
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
                ActivateThrow(fireball_1, ps1, rend1, ref fireball1ChargeTimer, ref fireball_1_recharging);
            }
            else
            {
                fireball_2_active = false;
                ActivateThrow(fireball_2, ps2, rend2, ref fireball2ChargeTimer, ref fireball_2_recharging);
            }
        }
        else
        {
            if (fireball_2_active)
            {
                fireball_2_active = false;
                ActivateThrow(fireball_2, ps2, rend2, ref fireball2ChargeTimer, ref fireball_2_recharging);
            }
            else
            {
                fireball_1_active = false;
                ActivateThrow(fireball_1, ps1, rend1, ref fireball1ChargeTimer, ref fireball_1_recharging);
            }
        }

        if (soundSource && flameShoot)
            soundSource.PlayOneShot(flameShoot);
    }

    void ActivateThrow(GameObject obj, ParticleSystem ps, Renderer rend, ref float chargeTimer, ref bool isRecharging)
    {
        GameObject fireball = Instantiate(FireballPref, obj.transform.position, Camera.main.transform.rotation);
        Fireball fb = fireball.GetComponent<Fireball>();

        if (upgradeOne) fb.splashRadius *= 1.5f;
        if (upgradeTwo) fb.setEnemiesOnFire = true;
        if (subRadiusUpgrade) fb.splashRadius *= 1.1f;
        if (subDamageUpgrade) fb.damage *= 1.1f;

        Destroy(fireball, 5f);

        // After throwing: NOT recharging until CollectFire() is called.
        chargeTimer = 0f;
        isRecharging = false;

        // While not recharged, keep them at 50% size + gray/black.
        ApplyUnchargedVisuals(obj, ps, rend);
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