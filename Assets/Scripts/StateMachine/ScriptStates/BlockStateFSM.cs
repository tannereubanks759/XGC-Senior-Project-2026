/*
 * BlockStateFSM.cs
 * 
 * This script defines the Block state for an enemy in the state machine.
 * While in this state, the enemy stops moving, faces the player, and plays 
 * a block animation. The enemy remains in this state until the animation 
 * finishes, then transitions to an appropriate next state depending on player distance.
 * 
 * By: Matthew Bolger
*/

using UnityEngine;

// Represents the Block state in the enemy's finite state machine.
// In this state, the enemy defends against player attacks by blocking.
public class BlockStateFSM : BaseState<EnemyState>
{
    // Reference to the enemy AI using this state.
    private BaseEnemyAI _enemy;

    // Constructor — initializes the state with its key, enemy reference, and animator.
    public BlockStateFSM(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    // Called once when entering the Block state.
    public override void EnterState()
    {
        //Debug.Log("Entered Block State");

        // Stop enemy movement and rotate to face the player.
        _enemy.StopMoving();
        _enemy.RotateToPlayer();

        // Reset animator triggers to prevent conflicts.
        _enemy.ResetTriggers();

        // Trigger the block animation.
        _enemy.Animator.SetTrigger("Block");

        // Mark the enemy as currently blocking.
        _enemy.isBlocking = true;
    }

    // Called once when leaving the Block state.
    // Resets animator triggers, resumes movement, and clears blocking status.
    public override void ExitState()
    {
        _enemy.ResetTriggers();
        _enemy.ResumeMoving();
        _enemy.isBlocking = false;
    }

    // Called every frame while in the Block state.
    public override void UpdateState()
    {
        // Placeholder for potential block behavior (e.g., slight movement backward)
        if (_enemy.isBlocking)
        {
            // _enemy.transform.position = -_enemy.transform.forward * Time.deltaTime;
        }
    }

    // Determines the next state based on time spent blocking and player distance.
    public override EnemyState GetNextState()
    {
        float dist = _enemy.DistanceToPlayer();

        // Check if the block animation has finished.
        if (!_enemy.isBlocking)
        {
            // If the player is within chase range...
            if (dist <= _enemy.ChaseRange)
            {
                // If the player is close enough to attack, transition to Attack.
                if (dist <= _enemy.AttackRange)
                {
                    return EnemyState.Attack;
                }
                // Otherwise, transition to Chase.
                return EnemyState.Chase;
            }

            // If the player is out of range, transition to Idle.
            return EnemyState.Idle;
        }

        // Otherwise, remain in Block state until animation finishes.
        return StateKey;
    }
}
