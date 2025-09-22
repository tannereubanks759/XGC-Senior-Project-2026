/*
 * ChaseStateFSM.cs
 * 
 * Defines the behavior of an enemy while it is 
 * actively pursuing the player. In this state, the enemy 
 * uses the NavMeshAgent to move toward the player's position 
 * until it either reaches attack range or the player escapes.
 * 
 * By: Matthew Bolger
 */

using UnityEngine;

// ChaseStateFSM inherits from BaseState<EnemyState>, meaning it represents
// the "Chase" state in the enemy’s finite state machine.
public class ChaseStateFSM : BaseState<EnemyState>
{
    // Reference to the enemy AI that owns this state.
    private BaseEnemyAI _enemy;

    // Constructor requires the enum key for this state and the enemy reference.
    public ChaseStateFSM(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    // Called once when the enemy first enters the Chase state.
    public override void EnterState()
    {
        //Debug.Log("Entered Chase State");

        // Trigger the chase animation.
        _enemy.Animator.SetTrigger("Chase");

        // Set the enemy’s speed to its running speed.
        _enemy.SetSpeed(_enemy.RunSpeed);
    }

    // Called once when the enemy leaves the Chase state.
    public override void ExitState()
    {
        //Debug.Log("Exiting Chase State");

        // Reset all animator triggers to prevent conflicts with the next state.
        _enemy.ResetTriggers();
    }

    // Called every frame while the enemy is in the Chase state.
    public override void UpdateState()
    {
        // Continuously move toward the player's current position.
        _enemy.MoveTo(_enemy.Player.position);
    }

    // Determines which state the enemy should transition to next.
    public override EnemyState GetNextState()
    {
        // The distance to the player
        float dist = _enemy.DistanceToPlayer();

        // Is the AI dead
        if (_enemy.currentHealth <= 0)
            return EnemyState.Dead;

        // If the player is close enough, transition to Attack or block.
        if (dist < _enemy.AttackRange)
        {
            var weight = Random.Range(0, 2);

            switch (weight)
            {
                // 50% chance to attack
                case 0:
                    return EnemyState.Attack;
                // 50% chance to dodge
                case 1:
                    return EnemyState.Block;
            }
        }

        // If the player is too far away, transition back to Idle.
        // A buffer (+2f) is added to prevent constant flip-flopping
        // when the player is near the chase boundary.
        if (dist > _enemy.ChaseRange + 2f)
            return EnemyState.Idle;

        // Otherwise, stay in the Chase state.
        return StateKey;
    }
}
