using UnityEngine;
[CreateAssetMenu(fileName = "New Artifact Inherited", menuName = "Inventory/Artifact3")]
public class artifactThree : ItemData
{
    
    public override void OnEquip(GameObject player)
    {
        //base.OnEquip();
        
        
        swordDamageDeterminer sd = player.GetComponent<swordDamageDeterminer>();
        sd.isLighting = true;
        Debug.Log("Applied artifact3 stuff");


        inventoryScript invScript;
        invScript = player.GetComponentInChildren<inventoryScript>();
        invScript.toggleInv();
    }
    public override void OnUnEquip(GameObject player)
    {
        //base.OnUnEquip();
        
        swordDamageDeterminer sd = player.GetComponent<swordDamageDeterminer>();
        sd.isLighting = false;
        
        Debug.Log("UNApplied artifact3 stuff");
    }
}
