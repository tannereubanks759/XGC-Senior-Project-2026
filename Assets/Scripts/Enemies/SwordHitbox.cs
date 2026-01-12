using System.Collections.Generic;
using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    [Header("Damage")]
    public float damage = 10f;


    private void OnTriggerEnter(Collider other)
    {
        
        // Simple "damage interface": look for something that can take damage.
        // You can replace this with your own Health component.
        if (other.tag == "Player")
        {
            var health = other.GetComponentInChildren<CombatController>();
            if (health != null)
            {
                health.TakeDamage((int)damage);
            }
            this.GetComponent<Collider>().enabled = false;
        }
        
    }
}

