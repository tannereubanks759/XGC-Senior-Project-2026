using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class CombatController : MonoBehaviour
{
    [Header("Controls")]
    public KeyCode primaryAttack = KeyCode.Mouse0;
    public KeyCode block_or_aim = KeyCode.Mouse1;
    public KeyCode dodge = KeyCode.Space; // must be holding block key

    [Header("Animation")]
    public Animator swordAnim;
    private bool swinging;
    private bool blocking;

    // Private
    private DodgeDash dodgeScript;
    private Rigidbody rb;
    private FirstPersonController controller;

    [Header("Health")]
    public int maxHealth = 100;
    public int health = 100;                // "real" health (target)
    public Slider healthSlider;

    // Smooth UI health (eases toward 'health')
    private float displayedHealth;          // what the slider shows
    private float healthVelocity;           // ref param for SmoothDamp
    [Range(0.03f, 0.6f)]
    public float healthSmoothTime = 0.18f;  // lower = snappier; higher = slower

    [Header("Passive Healing")]
    public bool passiveHealing = true;
    public float regenDelay = 3f;           // seconds to wait after taking damage
    public float regenTickInterval = 1f;    // 1 HP per second
    public int regenAmountPerTick = 1;

    private float lastDamageTime = -Mathf.Infinity;
    private float regenAccumulator = 0f;

    [Header("UI - Regen Heart")]
    public RawImage regenHeart;                         // assign in Inspector
    public Color heartRegenColor = new Color(0.35f, 1f, 0.35f, 1f);
    public Color heartDamageColor = new Color(1f, 0.25f, 0.25f, 1f);
    [Range(0f, 1f)] public float blinkMinAlpha = 0.25f;
    [Range(0f, 1f)] public float blinkMaxAlpha = 1f;
    [Min(0.01f)] public float blinkFrequency = 1.5f; // cycles per second

    [Header("UI - Damage Flash")]
    [Min(0.02f)] public float damageFadeTime = 0.6f; // SmoothDamp time
    private float damageAlpha = 0f;                  // 1 -> 0 after hit
    private float damageAlphaVel = 0f;               // SmoothDamp velocity


    void Start()
    {
        health = Mathf.Clamp(health, 0, maxHealth);
        displayedHealth = health;           // start in sync

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
            c.a = 0f;                // default invisible
            regenHeart.color = c;
        }

    }

    void Update()
    {
        if (FirstPersonController.isPaused) return;

        EnsureSlider();

        // --- Passive healing with cooldown and 1-HP ticks ---
        if (passiveHealing && health < maxHealth)
        {
            // Only start accumulating time after the cooldown since last damage
            if (Time.time - lastDamageTime >= regenDelay)
            {
                regenAccumulator += Time.deltaTime;

                // Add exactly 1 HP per full second accumulated (or whatever tick interval you set)
                while (regenAccumulator >= regenTickInterval && health < maxHealth)
                {
                    health = Mathf.Min(health + regenAmountPerTick, maxHealth);
                    regenAccumulator -= regenTickInterval;
                }
            }
        }
        else
        {
            // If at max or healing disabled, keep accumulator from drifting huge
            regenAccumulator = Mathf.Min(regenAccumulator, regenTickInterval);
        }

        // EASE the displayed health toward the target health
        displayedHealth = Mathf.SmoothDamp(
            displayedHealth,
            health,
            ref healthVelocity,
            healthSmoothTime
        );
        displayedHealth = Mathf.Clamp(displayedHealth, 0f, maxHealth);

        // Is regen currently active? (after cooldown + not full)
        bool isRegenerating = passiveHealing
                              && health < maxHealth
                              && (Time.time - lastDamageTime) >= regenDelay;

        // Ease damage flash alpha toward 0
        if (damageAlpha > 0.001f)
        {
            damageAlpha = Mathf.SmoothDamp(damageAlpha, 0f, ref damageAlphaVel, damageFadeTime);
            if (damageAlpha < 0.001f) { damageAlpha = 0f; damageAlphaVel = 0f; }
        }

        // Update the heart visual with priority: damage flash > regen > transparent
        UpdateRegenHeart(isRegenerating);



        if (healthSlider != null)
            healthSlider.value = displayedHealth;

        // --- Combat input ---
        if (Input.GetKey(primaryAttack))
        {
            swinging = true;
            swordAnim.SetBool("swinging", true);
        }
        else
        {
            swinging = false;
            swordAnim.SetBool("swinging", false);
        }

        if (Input.GetKey(block_or_aim))
        {
            blocking = true;
            swordAnim.SetBool("blocking", true);
        }
        else
        {
            blocking = false;
            swordAnim.SetBool("blocking", false);
        }

        if (swinging && blocking) // heavy attack
        {
            swordAnim.SetBool("heavy", true);
            controller.playerCanMove = false;
        }
        else
        {
            swordAnim.SetBool("heavy", false);
            controller.playerCanMove = true;
        }

        if (blocking && !swinging && Input.GetKeyDown(dodge))
        {
            Vector3 direction = rb.linearVelocity.normalized;
            dodgeScript.Dodge(direction);
        }

        if (Input.GetKeyDown(KeyCode.H)) // test damage
        {
            TakeDamage(10);
            Debug.Log("Current health " + health);
        }
        if (Input.GetKeyDown(KeyCode.J)) // test heal
        {
            Heal(10);
            Debug.Log("Current health " + health);
        }
    }

    void TakeDamage(int damage)
    {
        health = Mathf.Max(health - damage, 0);
        lastDamageTime = Time.time;   // reset regen cooldown
        regenAccumulator = 0f;        // reset regen tick build-up

        // Kick off damage flash
        damageAlpha = 1f;             // fully visible red
        damageAlphaVel = 0f;          // reset ease
    }


    public void Heal(int amount)
    {
        health = Mathf.Min(health + amount, maxHealth);
        // (Intentionally NOT resetting lastDamageTime—heals shouldn't delay passive regen)
    }

    void EnsureSlider()
    {
        if (healthSlider == null)
        {
            var go = GameObject.FindGameObjectWithTag("healthbar");
            if (go != null)
                healthSlider = go.GetComponent<Slider>();
            var h = GameObject.Find("Heart");
            if(h != null)
            {
                regenHeart = h.GetComponent<RawImage>();
            }
        }
    }

    void UpdateRegenHeart(bool regenActive)
    {
        if (regenHeart == null) return;

        // 1) Damage flash takes priority
        if (damageAlpha > 0f)
        {
            Color c = heartDamageColor;
            c.a = damageAlpha;
            regenHeart.color = c;
            return;
        }

        // 2) Green eased blink while regenerating
        if (regenActive)
        {
            // Smooth 0..1 with cosine (eases at ends)
            float p = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * blinkFrequency * Time.time);
            float a = Mathf.Lerp(blinkMinAlpha, blinkMaxAlpha, p);

            Color c = heartRegenColor;
            c.a = a;
            regenHeart.color = c;
            return;
        }

        // 3) Default: fully transparent
        Color idle = regenHeart.color;
        idle.a = 0f;
        regenHeart.color = idle;
    }


}
