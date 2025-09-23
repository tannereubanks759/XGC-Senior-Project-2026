using Unity.VisualScripting.FullSerializer;
using UnityEngine;
[CreateAssetMenu(fileName = "New Artifact Inherited", menuName = "Inventory/Artifact2")]
public class artifactTwo : ItemData
{
    public override void OnEquip(GameObject player)
    {
        //base.OnEquip();
        /* FirstPersonController playercontrollerRef;
         playercontrollerRef = player.GetComponent<FirstPersonController>();
         playercontrollerRef.walkSpeed = 10;
         Debug.Log("Applied artifact2 stuff");*/
        //BaseEnemyAI enemyControllerRef;
        swordDamageDeterminer sd= player.GetComponent<swordDamageDeterminer>();
        sd.damage = 50;
        
        inventoryScript invScript;
        invScript= player.GetComponentInChildren<inventoryScript>();
        invScript.toggleInv();
    }
    public override void OnUnEquip(GameObject player)
    {
        //base.OnUnEquip();
        /* FirstPersonController playercontrollerRef;
         playercontrollerRef = player.GetComponent<FirstPersonController>();
         playercontrollerRef.walkSpeed = 5;*/

        swordDamageDeterminer sd = player.GetComponent<swordDamageDeterminer>();
        sd.damage = 10;
        Debug.Log("UNApplied artifact2 stuff");
    }
}
