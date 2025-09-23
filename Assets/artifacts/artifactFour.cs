using UnityEngine;
[CreateAssetMenu(fileName = "New Artifact Inherited", menuName = "Inventory/ArtifactFour")]
public class artifactFour : ItemData
{

    public override void OnEquip(GameObject player)
    {
        //base.OnEquip();
        FirstPersonController playercontrollerRef;
        playercontrollerRef = player.GetComponent<FirstPersonController>();
        playercontrollerRef.fov = 120;
        inventoryScript invScript;
        invScript = player.GetComponentInChildren<inventoryScript>();
        invScript.toggleInv();

    }
    public override void OnUnEquip(GameObject player)
    {
        FirstPersonController playercontrollerRef;
        playercontrollerRef = player.GetComponent<FirstPersonController>();
        playercontrollerRef.fov = 60;
    }
}