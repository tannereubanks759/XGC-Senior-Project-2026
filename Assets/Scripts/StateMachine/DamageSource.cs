using UnityEngine;

public class DamageSource : MonoBehaviour
{
    public int damageAmount = 10;

    private void OnTriggerEnter(Collider other)
    {
        IDamageable damageable = other.GetComponent<IDamageable>();

        if (damageable != null )
        {
            damageable.TakeDamage( damageAmount );
        }
    }
}
