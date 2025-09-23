using UnityEngine;
[CreateAssetMenu(fileName = "New Artifact Inherited", menuName = "Inventory/Artifact3")]
public class artifactThree : ItemData
{
    
    public override void OnEquip(GameObject player)
    {
        //base.OnEquip();
        FirstPersonController playercontrollerRef;
        playercontrollerRef = player.GetComponent<FirstPersonController>();
        playercontrollerRef.unlimitedSprint = true;
        Debug.Log("Applied artifact3 stuff");
        inventoryScript invScript;
        invScript = player.GetComponentInChildren<inventoryScript>();
        invScript.toggleInv();
    }
    public override void OnUnEquip(GameObject player)
    {
        //base.OnUnEquip();
        FirstPersonController playercontrollerRef;
        playercontrollerRef = player.GetComponent<FirstPersonController>();
        playercontrollerRef.unlimitedSprint = false;
        Debug.Log("UNApplied artifact3 stuff");
    }
}
