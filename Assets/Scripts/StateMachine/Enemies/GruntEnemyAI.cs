using System.Xml;
using UnityEngine;

public class GruntEnemyAI : BaseEnemyAI
{
    void Awake()
    {
        base.Awake();

        PatrolArea area = FindClosestPatrolArea();

        Collider sword = GetComponentInChildren<AffectPlayer>().swordCollider;
        
        States[EnemyState.Idle] = new IdleStateFSM(EnemyState.Idle, this);
        States[EnemyState.Patrol] = new PatrolStateFSM(EnemyState.Patrol, this, area);
        States[EnemyState.Chase] = new ChaseStateFSM(EnemyState.Chase, this);
        States[EnemyState.Attack] = new AttackStateFSM(EnemyState.Attack, this, sword);
        States[EnemyState.Hit] = new HitStateFSM(EnemyState.Hit, this);
        States[EnemyState.Dead] = new DeadStateFSM(EnemyState.Dead, this);
        States[EnemyState.Block] = new BlockStateFSM(EnemyState.Block, this, this.Animator);
        States[EnemyState.BackDodge] = new BackDodgeStateFSM(EnemyState.BackDodge, this);

        CurrentState = States[EnemyState.Patrol]; // Start in patrol
    }
}