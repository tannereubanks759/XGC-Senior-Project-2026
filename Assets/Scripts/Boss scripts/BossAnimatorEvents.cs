using UnityEngine;

public class BossAnimatorEvents : MonoBehaviour
{
    private AnchorWeapon weapon;
    private BossHand hand;
    public ParticleSystem lavaPs;
    
    private void Start()
    {
        weapon = GetComponentInChildren<AnchorWeapon>();
        hand = GetComponentInChildren<BossHand>();
        if (lavaPs)
        {
            lavaPs.Stop();
        }
    }
    public void SetColliderOn()
    {
        if (weapon)
        {
            weapon.EnableCollider(true);
        }
        if (hand)
        {
            hand.EnableCollider(true);
        }
        
    }
    public void SetColliderOff()
    {
        if (weapon)
        {
            weapon.EnableCollider(false);
        }
        if (hand)
        {
            hand.EnableCollider(false);
        }
            
    }
    public void ThrowAnchor()
    {
        if (weapon)
        {
            weapon.Throw();
        }
        
    }

    public void SetLavaOn()
    {
        if (lavaPs)
        {
            if(lavaPs.isPlaying == false)
            {
                lavaPs.Play();
            }
            else
            {
                var emmission = lavaPs.emission;
                emmission.enabled = true;
            }
            
        }
        
    }
    public void SetLavaOff()
    {
        if (lavaPs)
        {
            var emmission = lavaPs.emission;
            emmission.enabled = false;
        }
        
    }
}
