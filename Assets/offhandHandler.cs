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
        foreach (ItemData item in allOffhands)
        {
            if (item != null)
                item.OnUnEquip(player);
        }
        currentOffhand = null;

    }
    public void lightning()
    {
        player = GameObject.FindWithTag("Player");
        unequip();
        Debug.Log("Lightning Offhand Swapped to");

        // base lightning
        if (allOffhands.Length > 1 && allOffhands[1] != null)
        {
            allOffhands[1].OnEquip(player);
            currentOffhand = allOffhands[1];
        }

        // upgrade 1
        if (lightningUpgradeCount >= 1 && allOffhands.Length > 2 && allOffhands[2] != null)
        {
            allOffhands[2].OnEquip(player);
        }

        // upgrade 2
        if (lightningUpgradeCount >= 2 && allOffhands.Length > 3 && allOffhands[3] != null)
        {
            allOffhands[3].OnEquip(player);
        }
    }
    public void chaos()
    {
        player = GameObject.FindWithTag("Player");
        unequip();
        
    }
    public void Defense()
    {
        player = GameObject.FindWithTag("Player");
        unequip();

        Debug.Log("Defense Offhand Swapped to");

        if (allOffhands.Length > 0 && allOffhands[0] != null)
        {
            allOffhands[0].OnEquip(player);
            currentOffhand = allOffhands[0];
        }
    }
    public void FireBomb()
    {
        player = GameObject.FindWithTag("Player");
        unequip();
        
    }
    public void increaseUpgradeStatus(int num)
    {
        if (num == 1)
        {
            lightningUpgradeCount++;
            lightning();        
        }
        else if (num == 2)
        {
            firebombUpgradeCount++;
            FireBomb();
        }
        else if (num == 3)
        {
            defenseUpgradeCount++;
            Defense();
        }
        else
        {
            choasUpgradeCount++;
            chaos();
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
