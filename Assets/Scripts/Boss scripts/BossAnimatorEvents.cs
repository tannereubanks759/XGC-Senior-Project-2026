using UnityEngine;

public class BossAnimatorEvents : MonoBehaviour
{
    private AnchorWeapon weapon;
    private BossHand hand;
    public ParticleSystem lavaPs;
    public Collider chargeAttackCol;
    public ParticleSystem spitPs;
    private void Start()
    {
        weapon = GetComponentInChildren<AnchorWeapon>();
        hand = GetComponentInChildren<BossHand>();
        if (lavaPs)
        {
            lavaPs.Stop();
        }
        if (spitPs)
        {
            spitPs.Stop();
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
            var emmission = lavaPs.emission;
            emmission.enabled = true;
            if (lavaPs.isPlaying == false)
            {
                lavaPs.Stop();
                lavaPs.Clear();
                lavaPs.Play();
            }
            
        }
    }

    public void SetSpitOn()
    {
        if (spitPs)
        {
            var emmission = spitPs.emission;
            emmission.enabled = true;
            if (spitPs.isPlaying == false)
            {
                spitPs.Stop();
                spitPs.Clear();
                spitPs.Play();
            }
        }
    }
    public void SetSpitOff()
    {
        if (spitPs)
        {
            var emmission = spitPs.emission;
            emmission.enabled = false;
        }
    }
    public void SetChargeOn()
    {
        if (chargeAttackCol)
        {
            chargeAttackCol.enabled = true;
        }
    }
    public void SetChargeOff()
    {
        if (chargeAttackCol)
        {
            chargeAttackCol.enabled = false;
        }
    }
    public void SetLavaOff()
    {
        if (lavaPs)
        {
            var emmission = lavaPs.emission;
            emmission.enabled = false;
        }
        if (chargeAttackCol)
        {
            chargeAttackCol.enabled = false;
        }
        
    }
}
