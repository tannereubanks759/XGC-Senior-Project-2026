using UnityEngine;
[CreateAssetMenu(fileName = "New Artifact Inherited", menuName = "Inventory/NewItem")]
public class artifactOne : ItemData
{
    //GameObject PlayerController;
    public override void OnEquip()
    {
        base.OnEquip();
        Debug.Log("Applied artifact1 stuff");
    }
    public override void OnUnEquip()
    {
        base.OnUnEquip();
        Debug.Log("UNApplied artifact1 stuff");
    }
}
