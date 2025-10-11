using UnityEngine;

public class BasicSkeleton : BaseEnemyAI
{
    private void Awake()
    {
        base.Awake();

        PatrolArea area = FindClosestPatrolArea();

        States[EnemyState.Idle] = new BasicIdleState(EnemyState.Idle, this);
        States[EnemyState.Patrol] = new BasicPatrolState(EnemyState.Patrol, this, area);
        States[EnemyState.Chase] = new BasicChaseState(EnemyState.Chase, this);
        States[EnemyState.Attack] = new BasicAttackState(EnemyState.Attack, this);

        CurrentState = States[EnemyState.Idle];
    }
}
