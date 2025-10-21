using UnityEngine;

[AddComponentMenu("AI/Boss Arena Trigger")]
[RequireComponent(typeof(Collider))]
public class BossArenaTrigger : MonoBehaviour
{
    [Tooltip("Assign the boss AI in this arena.")]
    public PirateBossAI boss;
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
        if (boss == null) return;

        if (other.CompareTag("Player"))
        {
            boss.BeginEncounter(other.transform);
            smokeCollider.isTrigger = false;
            _fired = true;
        }
    }
    
}
