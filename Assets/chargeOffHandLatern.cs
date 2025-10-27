using UnityEngine;

public class chargeOffHandLatern : MonoBehaviour
{
    public WeaponsManager WeaponsManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        WeaponsManager = GetComponentInChildren<WeaponsManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void activate()
    {
        WeaponsManager.swapLantern();
    }
    public void deactivate()
    {
        WeaponsManager.swapLantern();
    }
    public void hitRegistered()
    {

    }
}
