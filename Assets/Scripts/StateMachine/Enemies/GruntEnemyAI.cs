/*
 * GruntEnemyAI.cs
 * 
 * This script defines a specific type of enemy: the "Grunt".
 * It inherits from BaseEnemyAI and initializes all the FSM states
 * for this enemy type. The Grunt starts patrolling in the closest
 * PatrolArea and has access to all base behaviors like Attack, Chase,
 * Hit, Block, BackDodge, and Dead.
 * 
 * By: Matthew Bolger
*/

using UnityEngine;

public class GruntEnemyAI : BaseEnemyAI
{
    // Awake is called when the script instance is loaded
    void Awake()
    {
        // Call base Awake to initialize base variables
        base.Awake();

        // Find the closest patrol area for this enemy
        PatrolArea area = FindClosestPatrolArea();

        // Get a reference to the enemy's sword collider (used for attack detection)
        Collider sword = GetComponentInChildren<AffectPlayer>().swordCollider;

        // Initialize all states in the dictionary with this enemy as context
        States[EnemyState.Idle] = new IdleStateFSM(EnemyState.Idle, this);
        States[EnemyState.Patrol] = new PatrolStateFSM(EnemyState.Patrol, this, area);
        States[EnemyState.Chase] = new ChaseStateFSM(EnemyState.Chase, this);
        States[EnemyState.Attack] = new AttackStateFSM(EnemyState.Attack, this, sword);
        States[EnemyState.Hit] = new HitStateFSM(EnemyState.Hit, this);
        States[EnemyState.Dead] = new DeadStateFSM(EnemyState.Dead, this);
        States[EnemyState.Block] = new BlockStateFSM(EnemyState.Block, this);
        States[EnemyState.BackDodge] = new BackDodgeStateFSM(EnemyState.BackDodge, this);
        States[EnemyState.BlockHit] = new BlockHitStateFSM(EnemyState.BlockHit, this);

        // Set the initial state to Patrol
        CurrentState = States[EnemyState.Patrol];
    }
}
