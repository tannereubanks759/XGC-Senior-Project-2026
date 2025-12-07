using System.Diagnostics;
using UnityEngine;

[AddComponentMenu("AI/Boss Arena Trigger")]
[RequireComponent(typeof(Collider))]
public class BossArenaTrigger : MonoBehaviour
{
    [Tooltip("Assign the boss AI in this arena.")]
    public PirateBossAI boss;
    public MagmaBossAI magmaBoss;
    public GhostBossAI ghostBoss;
    [Tooltip("If true, this trigger only fires once.")]
    public bool oneShot = true;
    bool _fired;
    public Collider smokeCollider;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_fired && oneShot) return;
        if (boss == null && magmaBoss == null && ghostBoss == null) return;

        if (other.CompareTag("Player"))
        {
            if (boss)
            {
                boss.BeginEncounter(other.transform);
            }
            if (magmaBoss)
            {
                magmaBoss.BeginEncounter(other.transform);
            }
            if (ghostBoss)
            {
                ghostBoss.BeginEncounter(other.transform);
            }
            if (smokeCollider)
            {
                smokeCollider.isTrigger = false;
            }
            
            _fired = true;
        }
    }
    
}
