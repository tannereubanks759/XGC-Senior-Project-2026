using System.Collections.Generic;
using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    [Header("Damage")]
    public float damage = 22f;

    [Tooltip("Knockback impulse applied to rigidbodies (optional).")]
    public float knockback = 4.0f;

    [Tooltip("Layers that can be damaged.")]
    public LayerMask hittableLayers = ~0;

    [Tooltip("If true, each swing can only hit each target once.")]
    public bool hitOncePerSwing = true;

    private bool _active;
    private SkeletonSwordEnemy _owner;
    private readonly HashSet<Collider> _hitThisSwing = new HashSet<Collider>();

    public void BeginHitWindow(SkeletonSwordEnemy owner)
    {
        _owner = owner;
        _active = true;
        _hitThisSwing.Clear();
    }

    public void EndHitWindow()
    {
        _active = false;
        _owner = null;
        _hitThisSwing.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_active) return;
        if (((1 << other.gameObject.layer) & hittableLayers) == 0) return;

        if (hitOncePerSwing && _hitThisSwing.Contains(other))
            return;

        _hitThisSwing.Add(other);

        // Simple "damage interface": look for something that can take damage.
        // You can replace this with your own Health component.
        var health = other.GetComponentInParent<IDamageable>();
        if (health != null)
        {
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 hitNormal = (other.transform.position - transform.position).normalized;

            health.TakeDamage(damage, hitPoint, hitNormal, _owner ? _owner.gameObject : null);
        }

        // Optional knockback if the target has a rigidbody
        var rb = other.attachedRigidbody;
        if (rb && !rb.isKinematic)
        {
            Vector3 dir = (other.transform.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
            rb.AddForce(dir.normalized * knockback, ForceMode.Impulse);
        }
    }
}

public interface IDamageable
{
    void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal, GameObject instigator);
}
