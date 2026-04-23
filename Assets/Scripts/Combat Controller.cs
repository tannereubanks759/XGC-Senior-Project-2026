using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CombatController : MonoBehaviour
{
    [Header("Controls")]
    public KeyCode primaryAttack = KeyCode.Mouse0;
    public KeyCode block_or_aim = KeyCode.Mouse1;
    public KeyCode dodge = KeyCode.Space; // must be holding block key

    [Header("Animation")]
    public Animator swordAnim;
    public bool swinging;
    public bool blocking { get; private set; }

    // Private
    private DodgeDash dodgeScript;
    private Rigidbody rb;
    private FirstPersonController controller;

    [Header("Health")]
    public int maxHealth = 100;
    public int health = 100;
    public Slider healthSlider;
    public HurtPostFXURP hurtFX;
    public CameraShake shake;
    public AudioSource audioSource;
    public AudioClip[] hurtClips;

    // Smooth UI health
    private float displayedHealth;
    private float healthVelocity;
    [Range(0.03f, 0.6f)]
    public float healthSmoothTime = 0.18f;

    [Header("UI - Low Health Image Fade")]
    public Image lowHealthImage; // assign your image in inspector
    [Range(0f, 1f)] public float lowHealthMaxAlpha = 1f;

    [Header("Passive Healing")]
    public bool passiveHealing = true;
    public float regenDelay = 3f;
    public float regenTickInterval = 1f;
    public int regenAmountPerTick = 1;

    private float lastDamageTime = -Mathf.Infinity;
    private float regenAccumulator = 0f;

    [Header("UI - Regen Heart")]
    public RawImage regenHeart;
    public Color heartRegenColor = new Color(0.35f, 1f, 0.35f, 1f);
    public Color heartDamageColor = new Color(1f, 0.25f, 0.25f, 1f);
    [Range(0f, 1f)] public float blinkMinAlpha = 0.25f;
    [Range(0f, 1f)] public float blinkMaxAlpha = 1f;
    [Min(0.01f)] public float blinkFrequency = 1.5f;
    public float blockEffectiveness = 50f;

    [Header("UI - Damage Flash")]
    [Min(0.02f)] public float damageFadeTime = 0.6f;
    private float damageAlpha = 0f;
    private float damageAlphaVel = 0f;
    private WeaponInertia wInertia;

    [Header("Stagger Settings")]
    public bool isStaggered = false;
    public float staggerUpwardBoost = 0.0f;
    public float staggerLockTime = 0.25f;

    [Header("Stagger Physics")]
    [Tooltip("Instant horizontal speed change for knockback (m/s).")]
    public float staggerSpeedChange = 7.5f;

    [Tooltip("Assign a 0/0 friction PhysicMaterial (Combine: Minimum). Optional but recommended.")]
    public PhysicsMaterial staggerLowFriction;

    [Header("Attacks")]
    public int AmountOfAttacks = 4;
    private int CachedAttack;
    public GameObject crosshair;
    public GameObject player;
    private swordDamageDeterminer sd;
    public float dodgeCooldown;

    [Header("Sounds")]
    private SwordSounds swordSoundScript;
    public AudioSource soundSource;
    public AudioSource hurtSource;
    public AudioClip dodgeClip;

    public bool isPaused;
    public bool invulnerability = false;
    private float nextTime;
    chargeBaseScript cbs;

    public BossHealthbar boss_healthbar;
    public LavaDamage lavaDMG;
    public UImanager um;
    public Animator healthAnim;

    void Start()
    {
        if(hurtSource == null)
        {
            hurtSource = soundSource;
        }
        nextTime = Time.time;
        isPaused = false;
        swordSoundScript = GetComponentInChildren<SwordSounds>();
        player = GameObject.FindGameObjectWithTag("Player");
        sd = player.GetComponent<swordDamageDeterminer>();
        cbs = FindAnyObjectByType<chargeBaseScript>();
        crosshair.SetActive(false);
        CachedAttack = Random.Range(1, AmountOfAttacks + 1);
        hurtFX = GetComponent<HurtPostFXURP>();

        health = Mathf.Clamp(health, 0, maxHealth);
        displayedHealth = health;

        dodgeScript = GetComponentInChildren<DodgeDash>();
        rb = GetComponentInParent<Rigidbody>();
        controller = rb.GetComponent<FirstPersonController>();

        EnsureSlider();

        if (healthSlider != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = maxHealth;
            healthSlider.value = displayedHealth;
        }

        if (regenHeart != null)
        {
            var c = regenHeart.color;
            c.a = 0f;
            regenHeart.color = c;
        }

        UpdateLowHealthImageAlpha();

        wInertia = GetComponentInChildren<WeaponInertia>();
    }

    void OnEnable()
    {
        EnsureSlider();

        health = Mathf.Clamp(health, 0, maxHealth);
        displayedHealth = health;
        healthVelocity = 0f;

        if (healthSlider != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = maxHealth;
            healthSlider.value = displayedHealth;
        }

        UpdateLowHealthImageAlpha();
    }

    void Update()
    {
        EnsureSlider();

        if (passiveHealing && health < maxHealth)
        {
            if (Time.time - lastDamageTime >= regenDelay)
            {
                regenAccumulator += Time.deltaTime;

                while (regenAccumulator >= regenTickInterval && health < maxHealth)
                {
                    health = Mathf.Min(health + regenAmountPerTick, maxHealth);
                    regenAccumulator -= regenTickInterval;
                }
            }
        }
        else
        {
            regenAccumulator = Mathf.Min(regenAccumulator, regenTickInterval);
        }

        displayedHealth = Mathf.SmoothDamp(
            displayedHealth,
            health,
            ref healthVelocity,
            healthSmoothTime
        );
        displayedHealth = Mathf.Clamp(displayedHealth, 0f, maxHealth);

        bool isRegenerating = passiveHealing
                              && health < maxHealth
                              && (Time.time - lastDamageTime) >= regenDelay;

        if (damageAlpha > 0.001f)
        {
            damageAlpha = Mathf.SmoothDamp(damageAlpha, 0f, ref damageAlphaVel, damageFadeTime);
            if (damageAlpha < 0.001f) { damageAlpha = 0f; damageAlphaVel = 0f; }
        }

        UpdateRegenHeart(isRegenerating);

        if (healthSlider != null)
            healthSlider.value = displayedHealth;

        UpdateLowHealthImageAlpha();

        if (FirstPersonController.isPaused || isPaused) return;

        if (Input.GetKey(block_or_aim))
        {
            crosshair.SetActive(true);
        }
        else
        {
            crosshair.SetActive(false);
        }

        if (swordAnim.gameObject.activeSelf)
        {
            if (Time.timeScale == 1f)
            {
                if (Input.GetKey(primaryAttack))
                {
                    int random = Random.Range(1, AmountOfAttacks + 1);
                    swinging = true;
                    swordAnim.SetBool("swinging", true);
                    swordAnim.SetInteger("attackNum", random);
                }
                else
                {
                    swordAnim.SetInteger("attackNum", 0);
                    swinging = false;
                    swordAnim.SetBool("swinging", false);
                }

                if (Input.GetKey(block_or_aim) && !swordAnim.GetBool("Staggered"))
                {
                    blocking = true;
                    swordAnim.SetBool("blocking", true);
                }
                else
                {
                    blocking = false;
                    swordAnim.SetBool("blocking", false);
                }

                if (blocking && Input.GetKeyDown(primaryAttack))
                {
                    swordAnim.SetTrigger("Knockback");
                }
                else
                {
                    swordAnim.ResetTrigger("Knockback");
                }

                if (Time.time > nextTime && blocking && !swinging && Input.GetKeyDown(dodge))
                {
                    if (soundSource != null && dodgeClip != null)
                    {
                        soundSource.PlayOneShot(dodgeClip);
                    }
                    Vector3 direction = rb.linearVelocity.normalized;
                    dodgeScript.Dodge(direction);
                    nextTime = Time.time + dodgeCooldown;
                }
            }
        }

        if (health <= 20)
        {
            if (sd.bonusDamage)
            {
                sd.damage = 20;
            }
            else
            {
                sd.damage = 10;
            }
        }
        else
        {
            sd.damage = 10;
        }

        if (health <= 0)
        {
            Die();
        }
    }

    void UpdateLowHealthImageAlpha()
    {
        if (lowHealthImage == null) return;

        float halfHealth = maxHealth * 0.5f;
        float alpha = 0f;

        if (displayedHealth < halfHealth)
        {
            alpha = Mathf.InverseLerp(halfHealth, 0f, displayedHealth) * lowHealthMaxAlpha;
        }

        Color c = lowHealthImage.color;
        c.a = alpha;
        lowHealthImage.color = c;
    }

    public void Die()
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.ExitBossMusic(false);
        }
        LavaDamage lava = GetComponentInParent<LavaDamage>();
        lava.inLava = false;
        lava.inInk = false;
        if (boss_healthbar)
        {
            boss_healthbar.ResetHealthbar();
        }
        healthAnim.SetTrigger("Dead");

        GameObject.FindAnyObjectByType<UI>().ShowDeathScreen();
    }

    public void TakeDamage(int damage)
    {
        if (blocking == false)
        {
            if (invulnerability == false)
            {
                if(hurtClips.Length > 0)
                {
                    hurtSource.PlayOneShot(hurtClips[Random.Range(0, hurtClips.Length)]);
                }
                
                lastDamageTime = Time.time;
                regenAccumulator = 0f;

                damageAlpha = 1f;
                damageAlphaVel = 0f;
                int actuallyApplied = Mathf.Clamp(damage, 0, health);
                if (actuallyApplied <= 0) return;

                int old = health;
                health = Mathf.Max(0, health - actuallyApplied);

                Mathf.Clamp(old, 0, maxHealth);
                float severity = actuallyApplied / (float)old;
                if (!hurtFX) hurtFX = FindFirstObjectByType<HurtPostFXURP>();
                hurtFX?.Pulse(severity);
                shake.Shake(1);
            }
            else
            {
                swordSoundScript.PlayClashSound();
            }
        }
        else
        {
            if (controller.stamina < 2f)
            {
                swordAnim.SetBool("Staggered", true);
                swordAnim.SetTrigger("Stagger");
                DamageForced(damage);
            }
            else
            {
                wInertia.ParryClash(1);
            }
            swordSoundScript.PlayClashSound();
            controller.LoseStamina(2f);
        }
    }

    public void TakeDamage(int damage, Vector3 Dir)
    {
        if (blocking == false)
        {
            if (invulnerability == false)
            {
                audioSource.PlayOneShot(hurtClips[Random.Range(0, hurtClips.Length)]);
                cbs.decreaseCharge(3f);
                lastDamageTime = Time.time;
                regenAccumulator = 0f;

                damageAlpha = 1f;
                damageAlphaVel = 0f;
                int actuallyApplied = Mathf.Clamp(damage, 0, health);
                if (actuallyApplied <= 0) return;

                int old = health;
                health = Mathf.Max(0, health - actuallyApplied);

                Mathf.Clamp(old, 0, maxHealth);
                float severity = actuallyApplied / (float)old;
                if (!hurtFX) hurtFX = FindFirstObjectByType<HurtPostFXURP>();
                hurtFX?.Pulse(severity);
                shake.ShakeFromHit(Dir, 1);
            }
            else
            {
                swordSoundScript.PlayClashSound();
            }
        }
        else
        {
            if (controller.stamina < 2f)
            {
                swordAnim.SetBool("Staggered", true);
                swordAnim.SetTrigger("Stagger");
                DamageForced(damage);
            }
            else
            {
                wInertia.ParryClash(1);
            }
            swordSoundScript.PlayClashSound();
            controller.LoseStamina(2f);
        }
    }

    void DamageForced(int damage)
    {
        damage /= (int)(100f / blockEffectiveness);
        lastDamageTime = Time.time;
        regenAccumulator = 0f;

        damageAlpha = 1f;
        damageAlphaVel = 0f;
        int actuallyApplied = Mathf.Clamp(damage, 0, health);
        if (actuallyApplied <= 0) return;

        int old = health;
        health = Mathf.Max(0, health - actuallyApplied);

        Mathf.Clamp(old, 0, maxHealth);
        float severity = actuallyApplied / (float)old;
        if (!hurtFX) hurtFX = FindFirstObjectByType<HurtPostFXURP>();
        hurtFX?.Pulse(severity);
        shake.Shake(1);
    }

    public void TakeDamageByBoss(int damage)
    {
        audioSource.PlayOneShot(hurtClips[Random.Range(0, hurtClips.Length)]);
        if (blocking == false)
        {
            if (invulnerability == false)
            {
                cbs.decreaseCharge(3f);
                lastDamageTime = Time.time;
                regenAccumulator = 0f;

                damageAlpha = 1f;
                damageAlphaVel = 0f;
                int actuallyApplied = Mathf.Clamp(damage, 0, health);
                if (actuallyApplied <= 0) return;

                int old = health;
                health = Mathf.Max(0, health - actuallyApplied);

                Mathf.Clamp(old, 0, maxHealth);
                float severity = actuallyApplied / (float)old;
                if (!hurtFX) hurtFX = FindFirstObjectByType<HurtPostFXURP>();
                hurtFX?.Pulse(severity);
                shake.Shake(1);
            }
            else
            {
                swordSoundScript.PlayClashSound();
            }
        }
        else
        {
            DamageForced(damage);
            swordSoundScript.PlayClashSound();
            wInertia.ParryClash(1);
            controller.LoseStamina(4f);
        }
    }

    public void TDBB_With_Knockback(int damage, Transform from)
    {
        TakeDamageByBoss(damage);
        GetStaggeredFrom(from, 1f);
    }

    public void Heal(int amount)
    {
        health = Mathf.Min(health + amount, maxHealth);
    }

    void EnsureSlider()
    {
        if (healthSlider == null)
        {
            var go = GameObject.FindGameObjectWithTag("healthbar");
            if (go != null)
                healthSlider = go.GetComponent<Slider>();

            var h = GameObject.Find("Heart");
            if (h != null)
            {
                regenHeart = h.GetComponent<RawImage>();
            }
        }
    }

    void UpdateRegenHeart(bool regenActive)
    {
        if (regenHeart == null) return;

        if (damageAlpha > 0f)
        {
            Color c = heartDamageColor;
            c.a = damageAlpha;
            regenHeart.color = c;
            return;
        }

        if (regenActive)
        {
            float p = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * blinkFrequency * Time.time);
            float a = Mathf.Lerp(blinkMinAlpha, blinkMaxAlpha, p);

            Color c = heartRegenColor;
            c.a = a;
            regenHeart.color = c;
            return;
        }

        Color idle = regenHeart.color;
        idle.a = 0f;
        regenHeart.color = idle;
    }

    public void GetStaggered()
    {
        if (wInertia != null)
            wInertia.BlockStagger(1f);

        isStaggered = true;
        swordAnim.SetTrigger("Stagger");
        StartCoroutine(CoApplyStaggerKnockback(-transform.forward));
    }

    public void GetStaggeredFrom(Transform enemy, float intensity = 1f)
    {
        if (wInertia != null)
        {
            Vector3 fromEnemyToPlayer = (transform.position - enemy.position).normalized;
            isStaggered = true;
            StartCoroutine(CoApplyStaggerKnockback(fromEnemyToPlayer));
        }
        else
        {
            StartCoroutine(CoApplyStaggerKnockback(transform.position - enemy.position));
        }
    }

    private IEnumerator CoApplyStaggerKnockback(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = -transform.forward;
        dir.Normalize();

        Collider col = rb ? rb.GetComponent<Collider>() : null;
        PhysicsMaterial originalMat = null;
        if (col != null && staggerLowFriction != null)
        {
            originalMat = col.material;
            col.material = staggerLowFriction;
        }

        if (controller != null) controller.playerCanMove = false;

        yield return new WaitForFixedUpdate();

        Vector3 dV = dir * staggerSpeedChange + Vector3.up * Mathf.Max(0.0f, staggerUpwardBoost);
        rb.AddForce(dV, ForceMode.VelocityChange);

        yield return new WaitForSeconds(staggerLockTime);
        isStaggered = false;

        if (controller != null) controller.playerCanMove = true;
        if (col != null && staggerLowFriction != null) col.material = originalMat;
    }
}