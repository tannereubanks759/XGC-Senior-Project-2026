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
    private GameObject player;
    void Start()
    {
        //player = GameObject.FindWithTag("Player");
    }
    public void unequip()
    {
        player = GameObject.FindWithTag("Player");
        if (currentOffhand != null) 
        {
            currentOffhand.OnUnEquip(player);
        }
    }
    public void lightning()
    {
        unequip();
        currentOffhand = null;
        player = GameObject.FindWithTag("Player");
        Debug.Log("Lightning Offhand Swapped to");
        currentOffhand = allOffhands[1];
        if(currentOffhand != null) 
        {
            currentOffhand.OnEquip(player);
        }
    }
    public void chaos()
    {
        unequip();
        currentOffhand = null;
        player = GameObject.FindWithTag("Player");
        Debug.Log("Chaos Offhand Swapped to");
        if (currentOffhand != null)
        {
            currentOffhand.OnEquip(player);
        }
    }
    public void Defense()
    {
        unequip();
        currentOffhand = null;
        player = GameObject.FindWithTag("Player");
        Debug.Log("Defense Offhand Swapped to");
        currentOffhand = allOffhands[0];
        if (currentOffhand != null)
        {
            currentOffhand.OnEquip(player);
        }
    }
    public void FireBomb()
    {
        unequip();
        currentOffhand = null;
        player = GameObject.FindWithTag("Player");
        Debug.Log("Firebomb Offhand Swapped to");
        if (currentOffhand != null)
        {
            currentOffhand.OnEquip(player);
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
