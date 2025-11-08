using RayFire;
using UnityEngine;

public class EliteSkeleton : BaseEnemyAI
{
    [Header("Elite Specific Data")]
    [Tooltip("Controls how the enemy's attack effects the player")]
    [SerializeField] private EliteType type;

#pragma warning disable CS0114 // Member hides inherited member; missing override keyword
    private void Awake()
#pragma warning restore CS0114 // Member hides inherited member; missing override keyword
    {
        base.Awake();

        eliteType = type;

        PatrolArea area = FindClosestPatrolArea();
        RayfireRigid rf = GetComponentInChildren<RayfireRigid>();
        GameObject sword = GetComponentInChildren<Rigidbody>().gameObject;

        States[EnemyState.Idle] = new BasicIdleState(EnemyState.Idle, this);
        States[EnemyState.Patrol] = new BasicPatrolState(EnemyState.Patrol, this, area);
        States[EnemyState.Chase] = new BasicChaseState(EnemyState.Chase, this);
        States[EnemyState.Attack] = new BasicAttackState(EnemyState.Attack, this);
        States[EnemyState.Hit] = new BasicHitState(EnemyState.Hit, this);
        States[EnemyState.Dead] = new BasicDeadState(EnemyState.Dead, this, rf, sword);

        CurrentState = States[EnemyState.Idle];
    }
}

public enum EliteType
{
    Basic,
    Fire,
    Water,
    Gas,
}
