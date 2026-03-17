using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class chargeBaseScript : MonoBehaviour
{
    public float maxCharge = 100f;
    public float currentCharge = 0f;
    public bool isActive = false;

    [Header("Decay Settings")]
    public float decayAmountPerTick = 2f;
    public float decayInterval = 1f;

    [Header("UI")]
    // Text display is now optional — leave unassigned if you've fully replaced it with the sphere.
    public TextMeshProUGUI textCharge;

    [Tooltip("Drag in the ChargeSphere GameObject (the one with ChargeSphereUI on it).")]
    public ChargeSphereUI sphereUI;

    private float decayTimer = 0f;

    void Start()
    {
        if (textCharge != null)
            textCharge.text = "0";
    }

    public void increaseCharge(float amount)
    {
        if (currentCharge >= maxCharge)
        {
            currentCharge = maxCharge;
            return;
        }
        currentCharge = Mathf.Clamp(currentCharge + amount, 0f, maxCharge);
        updateVFX(currentCharge);

        if (textCharge != null)
            textCharge.text = currentCharge.ToString();
    }

    private void NaturalDecayTick()
    {
        if (currentCharge <= 0f) return;
        currentCharge = Mathf.Max(0f, currentCharge - decayAmountPerTick);
        updateVFX(currentCharge);

        if (textCharge != null)
            textCharge.text = currentCharge.ToString();
    }

    public void decreaseCharge(float amount)
    {
        if (currentCharge > 0)
        {
            currentCharge = Mathf.Clamp(currentCharge - amount, 0f, maxCharge);
            if (currentCharge < 0) currentCharge = 0;
            updateVFX(currentCharge);

            if (textCharge != null)
                textCharge.text = currentCharge.ToString();
        }
        else
        {
            currentCharge = 0;
            if (textCharge != null)
                textCharge.text = currentCharge.ToString();
        }
    }

    public void fullReset()
    {
        currentCharge = 0;
        updateVFX(currentCharge);

        if (textCharge != null)
            textCharge.text = currentCharge.ToString();
    }

    public void updateVFX(float charge)
    {
        // Notify the sphere UI whenever charge changes.
        if (sphereUI != null)
            sphereUI.OnChargeChanged(charge, maxCharge);
    }

    void Update()
    {
        if (currentCharge <= 0f) return;
        decayTimer += Time.deltaTime;
        if (decayTimer >= decayInterval)
        {
            decayTimer -= decayInterval;
            NaturalDecayTick();
        }
    }
}