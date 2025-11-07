using UnityEngine;
using UnityEngine.UI;
public class offhandHandler : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public ItemData currentOffhand;
    public ItemData[] allOffhands;
    public int lightningUpgradeCount = 0;
    public int choasUpgradeCount = 0;
    public int defenseUpgradeCount = 0;
    public int firebombUpgradeCount = 0;
    void Start()
    {
        
    }
   
    public void lightning()
    {
        Debug.Log("Lightning Offhand Swapped to");
    }
    public void chaos()
    {
        Debug.Log("Chaos Offhand Swapped to");
    }
    public void Defense()
    {
        Debug.Log("Defense Offhand Swapped to");
    }
    public void FireBomb()
    {
        Debug.Log("Firebomb Offhand Swapped to");
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
