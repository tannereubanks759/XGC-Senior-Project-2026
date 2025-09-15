using UnityEngine;
[CreateAssetMenu(fileName = "New Artifact Inherited", menuName = "Inventory/Artifact2")]
public class artifactTwo : ItemData
{
    public override void OnEquip()
    {
        base.OnEquip();
        Debug.Log("Applied artifact2 stuff");
    }
    public override void OnUnEquip()
    {
        base.OnUnEquip();
        Debug.Log("UNApplied artifact2 stuff");
    }
}
