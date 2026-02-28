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
    public FirstPersonController firstPersonController;

    [Header("Stamina Gating")]
    public float lowStaminaThreshold = 3.5f;

    [Tooltip("How long it takes to blend visuals into/out of low-stamina mode (seconds).")]
    public float lowStaminaBlendTime = 0.18f;

    [Header("Charging / Scale")]
    public float totalChargeTime = 8f;
    [Range(0f, 1f)] public float scaleAtPopStart = 0.6f;
    [Range(0f, 1f)] public float popStartTimeFraction = 0.75f;

    [Header("Uncharged Visuals")]
    [Range(0f, 1f)] public float unchargedScale = 0.5f;
    public Color unchargedColor = new Color(0.15f, 0.15f, 0.15f, 1f);

    [Header("Particle Speed")]
    public float chargingSimSpeed = 0.35f;
    public float fullSimSpeed = 1f;

    [Header("Start Settings")]
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

    public bool upgradeOne = false;
    public bool upgradeTwo = false;

    private ParticleSystem ps1;
    private ParticleSystem ps2;

    private Renderer rend1;
    private Renderer rend2;
    private Color baseColor1;
    private Color baseColor2;

    [Header("Read-Only State")]
    public bool bothReadyOrRecharging;

    // 0 = normal visuals, 1 = low-stamina visuals (blends smoothly)
    private float lowStaminaBlend = 0f;

    void Start()
    {
        parent.SetActive(false);
        targetScale = fireball_1.transform.localScale;

        ps1 = fireball_1.GetComponentInChildren<ParticleSystem>();
        ps2 = fireball_2.GetComponentInChildren<ParticleSystem>();

        rend1 = fireball_1.GetComponentInChildren<Renderer>();
        rend2 = fireball_2.GetComponentInChildren<Renderer>();

        if (rend1 != null) baseColor1 = rend1.material.color;
        if (rend2 != null) baseColor2 = rend2.material.color;

        ApplyFullVisuals(fireball_1, ps1, rend1, baseColor1);
        ApplyFullVisuals(fireball_2, ps2, rend2, baseColor2);
    }

    void Update()
    {
        bothReadyOrRecharging =
            (fireball_1_active || fireball_1_recharging) &&
            (fireball_2_active || fireball_2_recharging);

        // --- Low stamina blend (smooth 0<->1) ---
        bool isLowStamina = firstPersonController != null && firstPersonController.stamina < lowStaminaThreshold;
        float target = isLowStamina ? 1f : 0f;
        float speed = (lowStaminaBlendTime <= 0.0001f) ? 999f : (1f / lowStaminaBlendTime);
        lowStaminaBlend = Mathf.MoveTowards(lowStaminaBlend, target, speed * Time.deltaTime);

        // Recharge timers/logic continues (so recharges still "progress"),
        // but low stamina blend will override the SCALE + sim speed visuals.
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

        // Apply blended visuals LAST so it always wins (smoothly)
        ApplyLowStaminaBlendedVisuals();

        if (!parent.activeSelf) return;
        if (Time.time < nextTime) return;

        // Prevent firing when low stamina (blend will also be 1 here)
        if (Input.GetKeyDown(useKey))
        {
            if (activeFireballs <= 0)
            {
                firstPersonController.GetComponentInChildren<PopUpMessage>().ShowMessage("Recharge fireball near a torch");
                return;
            }
            if(firstPersonController.stamina < lowStaminaThreshold)
            {
                firstPersonController.GetComponentInChildren<PopUpMessage>().ShowMessage("Stamina too low");
                return;
            }
            Throw();

        }
    }

    /// <summary>
    /// Blends scale + particle sim speed into/out of low stamina mode.
    /// - Size blends toward unchargedScale
    /// - Particle sim speed blends toward chargingSimSpeed
    /// - Color is NOT touched (retains current color) while lowStaminaBlend > 0
    /// </summary>
    void ApplyLowStaminaBlendedVisuals()
    {
        ApplyBlendToOneOrb(
            fireball_1, ps1,
            fireball_1_active, fireball_1_recharging, fireball1ChargeTimer);

        ApplyBlendToOneOrb(
            fireball_2, ps2,
            fireball_2_active, fireball_2_recharging, fireball2ChargeTimer);
    }

    void ApplyBlendToOneOrb(GameObject orb, ParticleSystem ps, bool isActive, bool isRecharging, float timer)
    {
        if (orb == null) return;

        // "Normal" (non-low-stamina) scale factor:
        float normalScaleFactor;
        if (isActive)
        {
            normalScaleFactor = 1f;
        }
        else if (isRecharging)
        {
            normalScaleFactor = ComputeRechargeScale01(timer);
        }
        else
        {
            normalScaleFactor = Mathf.Clamp01(unchargedScale);
        }

        // Low stamina target scale factor:
        float lowScaleFactor = Mathf.Clamp01(unchargedScale);

        float finalFactor = Mathf.Lerp(normalScaleFactor, lowScaleFactor, lowStaminaBlend);
        orb.transform.localScale = targetScale * finalFactor;

        // Particle sim speed: blend down toward chargingSimSpeed while low stamina
        if (ps != null)
        {
            float normalSim = ComputeNormalSimSpeed(isActive, isRecharging, timer, normalScaleFactor);
            float finalSim = Mathf.Lerp(normalSim, chargingSimSpeed, lowStaminaBlend);
            SetParticleSimSpeed(ps, finalSim);
        }
    }

    float ComputeRechargeScale01(float chargeTimer)
    {
        float t = Mathf.Clamp01(chargeTimer / totalChargeTime);

        float minS = Mathf.Clamp01(unchargedScale);
        float popS = Mathf.Max(scaleAtPopStart, minS);

        if (t <= popStartTimeFraction)
        {
            float linT = (popStartTimeFraction <= 0f) ? 1f : (t / popStartTimeFraction);
            return Mathf.Lerp(minS, popS, linT);
        }
        else
        {
            float denom = Mathf.Max(0.0001f, 1f - popStartTimeFraction);
            float u = (t - popStartTimeFraction) / denom;
            float eased = u * u * u;
            return Mathf.Lerp(popS, 1f, eased);
        }
    }

    float ComputeNormalSimSpeed(bool isActive, bool isRecharging, float timer, float currentScaleFactor)
    {
        if (isActive) return fullSimSpeed;

        if (!isRecharging)
        {
            // Empty & not recharging: slow mode
            return chargingSimSpeed;
        }

        // Recharging: mimic your old logic based on currentScaleFactor
        float minS = Mathf.Clamp01(unchargedScale);
        float popS = Mathf.Max(scaleAtPopStart, minS);

        if (currentScaleFactor <= popS)
        {
            return chargingSimSpeed;
        }
        else
        {
            float u = (currentScaleFactor - popS) / Mathf.Max(0.0001f, (1f - popS));
            float easedU = u * u;
            return Mathf.Lerp(chargingSimSpeed, fullSimSpeed, easedU);
        }
    }

    public void CollectFire()
    {
        if (!fireball_1_active && !fireball_1_recharging)
        {
            StartRecharge(fireball_1, ps1, rend1, ref fireball_1_recharging, ref fireball1ChargeTimer);
            return;
        }

        if (!fireball_2_active && !fireball_2_recharging)
        {
            StartRecharge(fireball_2, ps2, rend2, ref fireball_2_recharging, ref fireball2ChargeTimer);
            return;
        }
    }

    void StartRecharge(GameObject fb, ParticleSystem ps, Renderer rend, ref bool isRecharging, ref float chargeTimer)
    {
        isRecharging = true;
        chargeTimer = 0f;

        // Set uncharged visuals (color sets to gray/black), unless you're already in low stamina.
        // If you ARE in low stamina, your color will be "retained" only after this moment (we don't modify it during blend).
        ApplyUnchargedVisuals(fb, ps, rend);
    }

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

        float scale01 = ComputeRechargeScale01(chargeTimer);

        // ✅ Always drive recharge color (low stamina should NOT block this)
        if (rend != null)
        {
            float minS = Mathf.Clamp01(unchargedScale);

            // Map scale from [unchargedScale..1] -> [0..1] for color lerp
            float colorU = (scale01 - minS) / Mathf.Max(0.0001f, (1f - minS));
            Color c = Color.Lerp(unchargedColor, baseColor, Mathf.Clamp01(colorU));
            rend.material.color = c;
        }

        // Done charging
        if (t >= 1f && !isActive)
        {
            isActive = true;
            isRecharging = false;
            activeFireballs++;

            // ✅ Force final "fully charged" color even if low stamina
            if (rend != null)
                rend.material.color = baseColor;

            // (Scale + sim speed are still handled by your low stamina blender each Update)
            if (ps != null && lowStaminaBlend <= 0.001f)
                SetParticleSimSpeed(ps, fullSimSpeed);

            if (soundSource && flameCharge)
                soundSource.PlayOneShot(flameCharge, 2f);
        }
    }

    void ApplyUnchargedVisuals(GameObject fb, ParticleSystem ps, Renderer rend)
    {
        // Scale is overridden by the blender each Update; this is still fine for initialization.
        if (fb != null) fb.transform.localScale = targetScale * Mathf.Clamp01(unchargedScale);
        SetParticleSimSpeed(ps, chargingSimSpeed);
        if (rend != null) rend.material.color = unchargedColor;
    }

    void ApplyFullVisuals(GameObject fb, ParticleSystem ps, Renderer rend, Color baseColor)
    {
        // Scale is overridden by the blender each Update; this is still fine for initialization.
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
        firstPersonController.LoseStamina(lowStaminaThreshold);

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

        chargeTimer = 0f;
        isRecharging = false;

        // Empty state visuals (color goes gray/black here)
        ApplyUnchargedVisuals(obj, ps, rend);
    }

    public void upgradeSplashRadius() => subRadiusUpgrade = true;
    public void upgradeDamage() => subDamageUpgrade = true;
}