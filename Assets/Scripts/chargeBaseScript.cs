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
    //public GameObject textObj;
    public TextMeshProUGUI textCharge;
    private float decayTimer = 0f;
    void Start()
    {
        textCharge.text = "0";
    }
    public void increaseCharge(float amount) 
    {
        // if its fully charged
        if(currentCharge>=maxCharge) 
        { // just so charge is never above 100.
            currentCharge = maxCharge;
            return;
        }
        // if it isnt then increase.
        currentCharge = Mathf.Clamp(currentCharge + amount, 0f, maxCharge);
        updateVFX(currentCharge);
        textCharge.text = currentCharge.ToString();
        //check to see if 


    }
    private void NaturalDecayTick()
    {
        if (currentCharge <= 0f) return;
        currentCharge = Mathf.Max(0f, currentCharge - decayAmountPerTick);
        updateVFX(currentCharge);
        textCharge.text = currentCharge.ToString();
    }
    public void decreaseCharge(float amount) 
    {
        if(currentCharge>0)
        {
            currentCharge = Mathf.Clamp(currentCharge - amount, 0f, maxCharge);
            if (currentCharge < 0)
            {  currentCharge = 0; }
            updateVFX(currentCharge);
            textCharge.text = currentCharge.ToString();

        }
        else
        {
            currentCharge = 0;
            textCharge.text = currentCharge.ToString();
        }
    }
    public void fullReset()
    {
        currentCharge = 0;
        updateVFX(currentCharge);
        textCharge.text = currentCharge.ToString();
    }
    public void updateVFX(float charge)
    {

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
