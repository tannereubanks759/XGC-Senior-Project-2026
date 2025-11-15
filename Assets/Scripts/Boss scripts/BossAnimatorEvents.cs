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
        if (weapon)
        {
            weapon.EnableCollider(true);
        }
        
    }
    public void SetColliderOff()
    {
        if (weapon)
        {
            weapon.EnableCollider(false);
        }
            
    }
    public void ThrowAnchor()
    {
        if (weapon)
        {
            weapon.Throw();
        }
        
    }
}
