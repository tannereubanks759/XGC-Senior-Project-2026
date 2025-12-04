using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Boss Healthbar")]
public class BossHealthbar : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 300;
    public int currentHealth = 300;

    [Header("UI Components")]
    public Slider bossHealthSlider;
    public CanvasGroup bossHealthGroup;
    public TextMeshProUGUI text;

    [Tooltip("Optional UI element that fades when the boss is idle/dead.")]
    public bool autoHideWhenFull = true;
    public float hideDelay = 2f;
    public float fadeSpeed = 2f;

    // Smooth UI health (eases toward target)
    private float displayedHealth;
    private float healthVelocity;
    [Range(0.03f, 0.6f)] public float healthSmoothTime = 0.18f;

    private float lastDamageTime;
    private bool fadingOut = false;

    void Start()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        displayedHealth = currentHealth;
        EnsureSlider();

        if (bossHealthSlider != null)
        {
            bossHealthSlider.minValue = 0;
            bossHealthSlider.maxValue = maxHealth;
            bossHealthSlider.value = displayedHealth;
        }

        /*if (bossHealthGroup != null)
            bossHealthGroup.alpha = 0f; // Start hidden until damaged*/
    }

    public void ResetHealthbar()
    {
        // Reset logical health
        currentHealth = maxHealth;
        displayedHealth = maxHealth;
        healthVelocity = 0f;

        // Make sure we have a slider reference
        EnsureSlider();

        // Reset slider values
        if (bossHealthSlider != null)
        {
            bossHealthSlider.minValue = 0;
            bossHealthSlider.maxValue = maxHealth;
            bossHealthSlider.value = maxHealth;
        }
        // Immediately hide the healthbar UI
        if (bossHealthGroup != null)
        {
            bossHealthGroup.alpha = 0f;   // fully faded out
        }

        // Reset fade state so it won’t auto-fade in
        fadingOut = false;
        lastDamageTime = -9999f; // ensures HandleAutoFade treats it as idle/hidden
    }


    void Update()
    {
        // Smooth health bar motion
        displayedHealth = Mathf.SmoothDamp(
            displayedHealth,
            currentHealth,
            ref healthVelocity,
            healthSmoothTime
        );
        displayedHealth = Mathf.Clamp(displayedHealth, 0f, maxHealth);

        if (bossHealthSlider != null)
            bossHealthSlider.value = displayedHealth;

        HandleAutoFade();
    }

    void EnsureSlider()
    {
        if (bossHealthSlider == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag("BossHealthbar");
            if (go != null)
                bossHealthSlider = go.GetComponent<Slider>();
        }
    }
    /// <summary>
    /// Call this when the boss is triggered/aggroed to fade the healthbar back in.
    /// </summary>
    public void ShowHealthbarOnBossTriggered()
    {
        // Make sure slider exists (in case scene changed / reloaded)
        EnsureSlider();

        // Start from fully transparent so the fade-in is visible
        if (bossHealthGroup != null)
        {
            bossHealthGroup.alpha = 0f;
        }

        // Cancel any fade-out logic
        fadingOut = false;

        // This tells HandleAutoFade that we were "just used",
        // so it will drive the alpha toward 1 over time.
        lastDamageTime = Time.time;
        currentHealth = maxHealth;
    }

    void HandleAutoFade()
    {
        if (bossHealthGroup == null) return;

        // Fade in when damaged
        if (Time.time - lastDamageTime < hideDelay)
        {
            fadingOut = false;
            bossHealthGroup.alpha = Mathf.MoveTowards(bossHealthGroup.alpha, 1f, Time.deltaTime * fadeSpeed);
        }
        else if (autoHideWhenFull && currentHealth >= maxHealth)
        {
            fadingOut = true;
        }

        // Fade out after a delay or when boss dies
        if (fadingOut)
            bossHealthGroup.alpha = Mathf.MoveTowards(bossHealthGroup.alpha, 0f, Time.deltaTime * fadeSpeed);
    }

    /// <summary>
    /// Call this function from your boss script when taking damage.
    /// Example: bossHealthbar.TakeDamage(25);
    /// </summary>
    public void TakeDamage(int currentHealthFromBoss)
    {
        currentHealth = currentHealthFromBoss;

        // Optional: if boss dies, fade out UI
        if (currentHealth <= 0)
        {
            StartCoroutine(FadeOutOnDeath());
        }
        Debug.Log(currentHealth);
    }

    private IEnumerator FadeOutOnDeath()
    {
        yield return new WaitForSeconds(1f);
        if (bossHealthGroup != null)
        {
            while (bossHealthGroup.alpha > 0f)
            {
                bossHealthGroup.alpha -= Time.deltaTime * fadeSpeed;
                yield return null;
            }
        }
    }

    /// <summary>
    /// Heals the boss by a given amount (optional).
    /// </summary>
    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }
}
