using UnityEngine;
using UnityEngine.UI;

public class OxygenSystem : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Player camera used to detect if you're underwater.")]
    public Transform playerCamera;

    [Tooltip("UI Slider for oxygen (0..1).")]
    public Slider oxygenSlider;

    [Tooltip("Optional: a parent GameObject to hide/show (panel that contains the slider). If null, uses slider's GameObject.")]
    public GameObject oxygenUIRoot;

    [Header("Ocean / Oxygen Settings")]
    [Tooltip("World-space Y level of the ocean surface.")]
    public float oceanLevelY = 0f;

    [Tooltip("Seconds to go from full oxygen to empty while underwater.")]
    public float depleteSeconds = 12f;

    [Tooltip("Seconds to go from empty oxygen to full while above water.")]
    public float refillSeconds = 6f;

    [Header("Damage When Out Of Oxygen")]
    [Tooltip("How often to apply damage ticks when oxygen is 0.")]
    public float damageTickInterval = 1f;

    // 0..1
    [Range(0f, 1f)]
    public float oxygen01 = 1f;

    [Header("Player combat controller for taking damage")]
    public CombatController health;
    public float waterDamage = 10f;

    float damageTickTimer = 0f;

    void Reset()
    {
        oxygen01 = 1f;
    }

    void Awake()
    {
        if (oxygenSlider != null)
        {
            oxygenSlider.minValue = 0f;
            oxygenSlider.maxValue = 1f;
            oxygenSlider.value = oxygen01;
        }

        if (oxygenUIRoot == null && oxygenSlider != null)
            oxygenUIRoot = oxygenSlider.gameObject;

        SetUIVisible(false);
    }

    void Update()
    {
        if (playerCamera == null || oxygenSlider == null)
            return;

        bool underwater = playerCamera.position.y < oceanLevelY;

        if (underwater)
        {
            // Show UI while underwater (even if full, it’ll remain until you surface or you choose otherwise)
            SetUIVisible(true);

            // Deplete oxygen
            float drainRate = (depleteSeconds <= 0.0001f) ? 9999f : (1f / depleteSeconds);
            oxygen01 = Mathf.Clamp01(oxygen01 - drainRate * Time.deltaTime);

            // If out of oxygen, tick damage
            if (oxygen01 <= 0f)
            {
                damageTickTimer += Time.deltaTime;
                if (damageTickTimer >= damageTickInterval)
                {
                    damageTickTimer = 0f;
                    DamageTick(); // <- fill this in
                }
            }
            else
            {
                // reset timer so it doesn't instantly tick after reaching 0
                damageTickTimer = 0f;
            }
        }
        else
        {
            // Refill oxygen above water
            float refillRate = (refillSeconds <= 0.0001f) ? 9999f : (1f / refillSeconds);
            oxygen01 = Mathf.Clamp01(oxygen01 + refillRate * Time.deltaTime);

            // Hide bar when fully refilled
            if (oxygen01 >= 1f)
            {
                SetUIVisible(false);
            }

            damageTickTimer = 0f;
        }

        oxygenSlider.value = oxygen01;
    }

    void SetUIVisible(bool visible)
    {
        if (oxygenUIRoot == null) return;
        if (oxygenUIRoot.activeSelf != visible)
            oxygenUIRoot.SetActive(visible);
    }

    void DamageTick()
    {
        health.TakeDamage((int)waterDamage);
    }
}
