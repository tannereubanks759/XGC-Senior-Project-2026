using UnityEngine;
[CreateAssetMenu(fileName = "New Artifact Inherited", menuName = "Inventory/KnockbackArt")]
public class knockBackArtifact : ItemData
{
    public override void OnEquip(GameObject player)
    {
        //base.OnEquip();
        RaycastKnockback rcKB;
        rcKB = player.GetComponentInChildren<RaycastKnockback>();
        rcKB.upgradedKnockback = true;
        inventoryScript invScript;
        invScript = player.GetComponentInChildren<inventoryScript>();
        invScript.toggleInv();

    }
    public override void OnUnEquip(GameObject player)
    {
        RaycastKnockback rcKB;
        rcKB = player.GetComponentInChildren<RaycastKnockback>();
        rcKB.upgradedKnockback = false;

    }
}
