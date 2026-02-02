using System.Collections.Generic;
using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    [Header("Damage")]
    public float damage = 10f;

    private SkeletonSwordEnemy ownerSkeleton;

    
    private void Awake()
    {
        ownerSkeleton = GetComponentInParent<SkeletonSwordEnemy>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponentInChildren<CombatController>();
            if (player != null)
            {
                int dealt = Mathf.RoundToInt(damage);
                player.TakeDamage(dealt);
                if (ownerSkeleton != null &&
                    ownerSkeleton.isCursed &&
                    ownerSkeleton.curseReflectEnabled)
                {
                    int reflected =
                        Mathf.RoundToInt(dealt * ownerSkeleton.curseReflectPercent);

                    if (reflected > 0)
                    {
                        ownerSkeleton.ApplyDamage(reflected, canStun: true);
                    }
                }
            }
            GetComponent<Collider>().enabled = false;
        }
    }
}


