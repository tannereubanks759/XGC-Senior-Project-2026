using UnityEngine;
[CreateAssetMenu(fileName = "New Artifact Inherited", menuName = "Inventory/Artifact3")]
public class artifactThree : ItemData
{

    public override void OnEquip(GameObject player)
    {
        if (player == null) { Debug.LogError("artifactThree: player is null!"); return; }

        chargeBaseScript cbs = FindAnyObjectByType<chargeBaseScript>();
        if (cbs == null) { Debug.LogError("artifactThree: chargeBaseScript not found in scene!"); return; }

        swordDamageDeterminer sd = player.GetComponent<swordDamageDeterminer>();
        if (sd == null) { Debug.LogError("artifactThree: swordDamageDeterminer not on player!"); return; }

        sd.isLighting = true;
        cbs.isActive = true;
        Debug.Log("Applied artifact3 stuff");
        sd.lightningSwordEffect.SetActive(true);
    }
    public override void OnUnEquip(GameObject player)
    {
        //base.OnUnEquip();
        chargeBaseScript cbs = FindAnyObjectByType<chargeBaseScript>();
        swordDamageDeterminer sd = player.GetComponent<swordDamageDeterminer>();
        cbs.isActive = false;
        sd.isLighting = false;
        sd.lightningSwordEffect.SetActive(false);
        Debug.Log("UNApplied artifact3 stuff");
    }
}
