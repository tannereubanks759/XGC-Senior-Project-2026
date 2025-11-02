using RayFire;
using UnityEngine;

public class RangeSkeleton : BaseEnemyAI
{
    private void Awake()
    {
        base.Awake();

        PatrolArea area = FindClosestPatrolArea();
        RayfireRigid rayf = GetComponentInChildren<RayfireRigid>();
        GameObject gun = GetComponentInChildren<Rigidbody>().gameObject;

        States[EnemyState.Idle] = new BasicIdleState(EnemyState.Idle, this);
        States[EnemyState.Patrol] = new BasicPatrolState(EnemyState.Patrol, this, area);
        States[EnemyState.Chase] = new BasicChaseState(EnemyState.Chase, this);
        States[EnemyState.Attack] = new RangeAttackState(EnemyState.Attack, this);
        States[EnemyState.Dead] = new BasicDeadState(EnemyState.Dead, this, rayf, gun);

        CurrentState = States[EnemyState.Idle];
    }
}
