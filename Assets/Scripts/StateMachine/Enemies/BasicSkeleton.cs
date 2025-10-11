using UnityEngine;

public class BasicSkeleton : BaseEnemyAI
{
    private void Awake()
    {
        base.Awake();

        PatrolArea area = FindClosestPatrolArea();

        States[EnemyState.Idle] = new BasicIdleState(EnemyState.Idle, this);
        States[EnemyState.Patrol] = new BasicPatrolState(EnemyState.Patrol, this, area);

        CurrentState = States[EnemyState.Idle];
    }
}
