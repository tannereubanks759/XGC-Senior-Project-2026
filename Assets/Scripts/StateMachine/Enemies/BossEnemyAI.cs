using UnityEngine;

public class BossEnemyAI : BaseEnemyAI
{
    void Awake()
    {
        States[EnemyState.Idle] = new IdleState(EnemyState.Idle, this);
        States[EnemyState.Chase] = new ChaseState(EnemyState.Chase, this);
        // States[EnemyState.Attack] = new BossAttackState(EnemyState.Attack, this);
        States[EnemyState.Dead] = new DeadState(EnemyState.Dead, this);

        // Future boss states:
        // States[EnemyState.Phase2] = new PhaseTwoState(...);

        CurrentState = States[EnemyState.Idle];
    }

    public override void Attack()
    {
        Debug.Log($"{name} performs BOSS ATTACK!");
        // Trigger boss VFX/animations
    }

    public override void Die()
    {
        base.Die();
        Debug.Log("Play BOSS death cutscene...");
    }
}
