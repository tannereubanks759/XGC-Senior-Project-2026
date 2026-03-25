using UnityEngine;
[CreateAssetMenu(fileName = "New Artifact Inherited", menuName = "Inventory/KnockbackArt")]
public class knockBackArtifact : ItemData
{
    public override void OnEquip(GameObject player)
    {
        RaycastKnockback rcKB = player.GetComponentInChildren<RaycastKnockback>(true);
        if (rcKB == null)
        {
            return;
        }
        rcKB.upgradedKnockback = true;
    }

    public override void OnUnEquip(GameObject player)
    {
        RaycastKnockback rcKB = player.GetComponentInChildren<RaycastKnockback>(true);
        if (rcKB == null)
        {
            return;
        }
        rcKB.upgradedKnockback = false;
    }
}
