using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;
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
    private WeaponInertia wInertia;
    private Volume healthVolume;

    [Header("Stagger Settings")]
    public bool isStaggered = false;
    public float staggerUpwardBoost = 0.0f;  // small lift if you want (0 = none)
    public float staggerLockTime = 0.25f;    // time the player can't move
    [Header("Stagger Physics")]
    [Tooltip("Instant horizontal speed change for knockback (m/s).")]
    public float staggerSpeedChange = 7.5f;

    [Tooltip("Assign a 0/0 friction PhysicMaterial (Combine: Minimum). Optional but recommended.")]
    public PhysicsMaterial staggerLowFriction;


    void Start()
    {
        //rb.linearDamping = 0f; // tiny values like 0.02 are fine too

        healthVolume = GetComponent<Volume>();
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

        wInertia = GetComponent<WeaponInertia>();

        Enemies();

    }

    void Enemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (var enemy in enemies)
        {
            enemy.GetComponent<BaseEnemyAI>().Player = this.transform;
        }
    }


    void Update()
    {
        if (FirstPersonController.isPaused) return;

        if (Input.GetKeyDown(KeyCode.O)) 
        {
            GetStaggered();
        }
        

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

        // --- Combat input ---Only when sword is active
        if (swordAnim.gameObject.activeSelf)
        {
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
                if (isStaggered == false)
                {
                    controller.playerCanMove = true;
                }

            }

            if (blocking && !swinging && Input.GetKeyDown(dodge))
            {
                Vector3 direction = rb.linearVelocity.normalized;
                dodgeScript.Dodge(direction);
            }
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

        if(health <= 0)
        {
            Die();
        }
        if(health < 50)
        {
            healthVolume.weight = ((100-healthSlider.value)/100F);
        }
        else
        {
            healthVolume.weight = 0.01F;
        }
    }

    public void Die()
    {
        GameObject.FindAnyObjectByType<UI>().ShowDeathScreen();
    }
    public void TakeDamage(int damage)
    {
        if(blocking == false)
        {
            health = Mathf.Max(health - damage, 0);
            lastDamageTime = Time.time;   // reset regen cooldown
            regenAccumulator = 0f;        // reset regen tick build-up

            // Kick off damage flash
            damageAlpha = 1f;             // fully visible red
            damageAlphaVel = 0f;          // reset ease
        }
        else
        {
            wInertia.ParryClash(1);
        }
    }

    /*
     private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("EnemyWeapon"))
        {
            Debug.Log("Player Hit");
            other.GetComponent<Collider>().enabled = false;
            TakeDamage(other.GetComponentInParent<GruntEnemyAI>().Damage);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("EnemyWeapon"))
        {
            other.GetComponent<Collider>().enabled = true;
        }
    } 
     */

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


    public void GetStaggered()
    {
        // 1) Weapon thunk (no camera shake)
        if (wInertia != null)
            wInertia.BlockStagger(1f); // fallback uses camera/right
        isStaggered = true;
        swordAnim.SetTrigger("Stagger");
        // 2) Knockback handled in physics-friendly coroutine (does friction swap + lockout)
        StartCoroutine(CoApplyStaggerKnockback(-transform.forward));
    }


    public void GetStaggeredFrom(Transform enemy, float intensity = 1f)
    {
        if (wInertia != null)
        {
            Vector3 fromEnemyToPlayer = (transform.position - enemy.position).normalized;
            wInertia.BlockStagger(fromEnemyToPlayer, intensity);
            isStaggered = true;
            swordAnim.SetTrigger("Stagger");
            StartCoroutine(CoApplyStaggerKnockback(fromEnemyToPlayer)); // push away from enemy
        }
        else
        {
            StartCoroutine(CoApplyStaggerKnockback(transform.position - enemy.position));
        }
    }
    private IEnumerator CoApplyStaggerKnockback(Vector3 dir)
    {
        // Horizontal-only direction
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = -transform.forward;
        dir.Normalize();

        // Swap player collider to low-friction material during the stagger (optional but helps a lot)
        Collider col = rb ? rb.GetComponent<Collider>() : null;
        PhysicsMaterial originalMat = null;
        if (col != null && staggerLowFriction != null)
        {
            originalMat = col.material;      // note: Collider.material is an instance property
            col.material = staggerLowFriction;
        }

        // Lock movement so FPC doesn't overwrite velocity this frame
        if (controller != null) controller.playerCanMove = false;

        // Sync with physics step
        yield return new WaitForFixedUpdate();

        // Apply Δv (ignores mass) + small upward pop to decouple from ground friction
        Vector3 dV = dir * staggerSpeedChange + Vector3.up * Mathf.Max(0.0f, staggerUpwardBoost);
        rb.AddForce(dV, ForceMode.VelocityChange);

        // Hold lockout for the stagger duration
        yield return new WaitForSeconds(staggerLockTime);
        isStaggered = false;
        swordAnim.ResetTrigger("Stagger");
        // Restore movement & friction
        if (controller != null) controller.playerCanMove = true;
        if (col != null && staggerLowFriction != null) col.material = originalMat;
    }



}
