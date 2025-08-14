using System.Xml;
using UnityEngine;

public class GruntEnemyAI : BaseEnemyAI
{
    void Awake()
    {
        base.Awake();

        States[EnemyState.Idle] = new IdleState(EnemyState.Idle, this);
        States[EnemyState.Patrol] = new PatrolState(EnemyState.Patrol, this);
        States[EnemyState.Chase] = new ChaseState(EnemyState.Chase, this);
        States[EnemyState.Attack] = new AttackState(EnemyState.Attack, this);
        States[EnemyState.Dead] = new DeadState(EnemyState.Dead, this);

        CurrentState = States[EnemyState.Patrol]; // Start in patrol
    }
}
