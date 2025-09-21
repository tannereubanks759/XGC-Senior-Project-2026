using UnityEngine;
[CreateAssetMenu(fileName = "New Artifact Inherited", menuName = "Inventory/Artifact2")]
public class artifactTwo : ItemData
{
    public override void OnEquip(GameObject player)
    {
        //base.OnEquip();
        FirstPersonController playercontrollerRef;
        playercontrollerRef = player.GetComponent<FirstPersonController>();
        playercontrollerRef.walkSpeed = 10;
        Debug.Log("Applied artifact2 stuff");
    }
    public override void OnUnEquip(GameObject player)
    {
        //base.OnUnEquip();
        FirstPersonController playercontrollerRef;
        playercontrollerRef = player.GetComponent<FirstPersonController>();
        playercontrollerRef.walkSpeed = 5;
        Debug.Log("UNApplied artifact2 stuff");
    }
}
