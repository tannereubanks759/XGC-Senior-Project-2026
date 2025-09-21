using UnityEngine;
[CreateAssetMenu(fileName = "New Artifact Inherited", menuName = "Inventory/NewItem")]
public class artifactOne : ItemData
{
    
    public override void OnEquip(GameObject player)
    {
        //base.OnEquip();
         FirstPersonController playercontrollerRef;
        playercontrollerRef = player.GetComponent<FirstPersonController>();
        playercontrollerRef.jumpPower = 15;
         Debug.Log("Applied artifact1 stuff");
    }
    public override void OnUnEquip(GameObject player)
    {
        FirstPersonController playercontrollerRef;
        playercontrollerRef = player.GetComponent<FirstPersonController>();
        playercontrollerRef.jumpPower = 5;
        //base.OnUnEquip();
        Debug.Log("UNApplied artifact1 stuff");
    }
}
