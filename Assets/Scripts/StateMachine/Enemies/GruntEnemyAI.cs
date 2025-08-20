using System.Xml;
using UnityEngine;

public class GruntEnemyAI : BaseEnemyAI
{
    void Awake()
    {
        base.Awake();

        PatrolArea area = FindClosestPatrolArea();
        
        States[EnemyState.Idle] = new IdleState(EnemyState.Idle, this);
        States[EnemyState.Patrol] = new PatrolState(EnemyState.Patrol, this, area);
        States[EnemyState.Chase] = new ChaseState(EnemyState.Chase, this);
        States[EnemyState.Attack] = new AttackState(EnemyState.Attack, this);
        States[EnemyState.Hit] = new HitState(EnemyState.Hit, this);
        States[EnemyState.Dead] = new DeadState(EnemyState.Dead, this);

        CurrentState = States[EnemyState.Patrol]; // Start in patrol
    }
}
