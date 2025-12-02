using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class chargeBaseScript : MonoBehaviour
{
    public float maxCharge = 100f;
    public float currentCharge = 0f;
    public bool isActive = false;
    void Start()
    {
        
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
        //check to see if 


    }
    public void decreaseCharge(float amount) 
    {
        if(currentCharge>0)
        {
            currentCharge = Mathf.Clamp(currentCharge - amount, 0f, maxCharge);
            if (currentCharge < 0)
            {  currentCharge = 0; }
            updateVFX(currentCharge);

        }
        else
        {
            currentCharge = 0;
        }
    }
    public void fullReset()
    {
        currentCharge = 0;
        updateVFX(currentCharge);
    }
    public void updateVFX(float charge)
    {

    }
    void Update()
    {
        
    }
}
