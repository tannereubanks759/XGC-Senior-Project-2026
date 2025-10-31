using UnityEngine;

public class RangeSkeleton : BaseEnemyAI
{
    private void Awake()
    {
        base.Awake();

        PatrolArea area = FindClosestPatrolArea();

        States[EnemyState.Idle] = new BasicIdleState(EnemyState.Idle, this);
        States[EnemyState.Patrol] = new BasicPatrolState(EnemyState.Patrol, this, area);
        States[EnemyState.Chase] = new BasicChaseState(EnemyState.Chase, this);

        CurrentState = States[EnemyState.Idle];
    }
}
