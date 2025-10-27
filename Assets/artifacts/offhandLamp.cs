using UnityEngine;
[CreateAssetMenu(fileName = "New Artifact Inherited", menuName = "Inventory/OffhandLamp")]
public class offhandLamp : ItemData
{
    public override void OnEquip(GameObject player)
    {
        var script = player.GetComponent<chargeOffHandLatern>();
        script.activate();
        inventoryScript invScript;
        invScript = player.GetComponentInChildren<inventoryScript>();
        invScript.toggleInv();
    }
    public override void OnUnEquip(GameObject player)
    {
        var script = player.GetComponent<chargeOffHandLatern>();
        script.deactivate();
    }
}
