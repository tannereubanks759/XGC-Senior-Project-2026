using UnityEngine;

public class BossAnimatorEvents : MonoBehaviour
{
    private AnchorWeapon weapon;
    private void Start()
    {
        weapon = GetComponentInChildren<AnchorWeapon>();
    }
    public void SetColliderOn()
    {
        weapon.EnableCollider(true);
    }
    public void SetColliderOff()
    {
        weapon.EnableCollider(false);
    }
}
