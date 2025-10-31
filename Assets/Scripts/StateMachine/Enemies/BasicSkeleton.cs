using UnityEngine;
using RayFire;

public class BasicSkeleton : BaseEnemyAI
{
    private void Awake()
    {
        base.Awake();

        PatrolArea area = FindClosestPatrolArea();
        RayfireRigid rf = GetComponentInChildren<RayfireRigid>();
        GameObject sword = GetComponentInChildren<Rigidbody>().gameObject;
        States[EnemyState.Idle] = new BasicIdleState(EnemyState.Idle, this);
        States[EnemyState.Patrol] = new BasicPatrolState(EnemyState.Patrol, this, area);
        States[EnemyState.Chase] = new BasicChaseState(EnemyState.Chase, this);
        States[EnemyState.Attack] = new BasicAttackState(EnemyState.Attack, this);
        States[EnemyState.Block] = new BasicBlockState(EnemyState.Block, this);
        States[EnemyState.Hit] = new BasicHitState(EnemyState.Hit, this);
        States[EnemyState.Dead] = new BasicDeadState(EnemyState.Dead, this, rf, sword);

        CurrentState = States[EnemyState.Idle];

    }
}
