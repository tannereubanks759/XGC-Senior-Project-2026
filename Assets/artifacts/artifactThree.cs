using UnityEngine;
[CreateAssetMenu(fileName = "New Artifact Inherited", menuName = "Inventory/Artifact3")]
public class artifactThree : ItemData
{
    public override void OnEquip()
    {
        base.OnEquip();
        Debug.Log("Applied artifact3 stuff");
    }
    public override void OnUnEquip()
    {
        base.OnUnEquip();
        Debug.Log("UNApplied artifact3 stuff");
    }
}
