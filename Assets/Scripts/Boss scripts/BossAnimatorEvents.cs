using UnityEngine;

public class BossAnimatorEvents : MonoBehaviour
{
    private AnchorWeapon weapon;
    private BossHand hand;

    [Header("VFX")]
    public ParticleSystem lavaPs;
    public ParticleSystem spitPs;

    [Header("Colliders")]
    public Collider chargeAttackCol;

    private void Start()
    {
        weapon = GetComponentInChildren<AnchorWeapon>();
        hand = GetComponentInChildren<BossHand>();

        if (lavaPs)
        {
            lavaPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        if (spitPs)
        {
            spitPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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

    // ===== LAVA / CHARGE VFX =====

    public void SetLavaOn()
    {
        if (!lavaPs) return;

        // ALWAYS enable emission when turning on
        var emission = lavaPs.emission;
        emission.enabled = true;

        // Restart from a clean state
        lavaPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        lavaPs.Play(true);
    }

    public void SetLavaOff()
    {
        if (lavaPs)
        {
            var emission = lavaPs.emission;
            emission.enabled = false;
        }
        if (chargeAttackCol)
        {
            chargeAttackCol.enabled = false;
        }
    }

    public void SetChargeOn()
    {
        if (chargeAttackCol)
        {
            chargeAttackCol.enabled = true;
        }
    }

    // ===== SPIT VFX =====

    public void SetSpitOn()
    {
        if (!spitPs) return;

        var emission = spitPs.emission;
        emission.enabled = true;

        spitPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        spitPs.Play(true);
    }

    public void SetSpitOff()
    {
        if (spitPs)
        {
            var emission = spitPs.emission;
            emission.enabled = false;
        }
    }
}
