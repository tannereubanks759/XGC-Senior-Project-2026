/*
 * BackDodgeStateFSM.cs
 * 
 * This script defines the BackDodge state for an enemy in the state machine.
 * When the enemy enters this state, it quickly dodges backward to evade attacks,
 * stops normal movement, faces the player, and plays the back-dodge animation.
 * After the dodge is complete, the enemy transitions to an appropriate next state
 * based on player distance.
 * 
 * By: Matthew Bolger
*/

using UnityEngine;

// Represents the BackDodge state in the enemy's finite state machine.
// In this state, the enemy performs a backward dodge to evade attacks.
public class BackDodgeStateFSM : BaseState<EnemyState>
{
    // Reference to the enemy AI using this state.
    private BaseEnemyAI _enemy;

    // Constructor — initializes the state with its key and a reference to the enemy AI.
    public BackDodgeStateFSM(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    // Called once when entering the BackDodge state.
    // Stops movement, rotates to face the player, triggers animation, and flags dodging.
    public override void EnterState()
    {
        Debug.Log("Entered BackDodge State");

        // Stop movement and rotate to face the player.
        _enemy.StopMoving();
        _enemy.RotateToPlayer();

        // Reset all animation triggers to avoid conflicts.
        _enemy.ResetTriggers();

        // Mark the enemy as currently dodging.
        _enemy.isDodging = true;

        // Trigger the back-dodge animation.
        _enemy.Animator.SetTrigger("BackDodge");
    }

    // Called once when leaving the BackDodge state.
    // Resets animation triggers and allows the enemy to resume normal movement.
    public override void ExitState()
    {
        _enemy.ResetTriggers();
        _enemy.ResumeMoving();
    }

    // Called every frame while in the BackDodge state.
    public override void UpdateState()
    {
        // TODO: Implement actual backward movement during dodge.
        // Example: _enemy.transform.position -= _enemy.transform.forward * dodgeSpeed * Time.deltaTime;
    }

    // Determines the next state after the dodge is complete.
    public override EnemyState GetNextState()
    {
        float dist = _enemy.DistanceToPlayer();

        // If the player is within chase range and dodge is finished...
        if (dist <= _enemy.ChaseRange && !_enemy.isDodging)
        {
            // If the player is within attack range, transition to Attack.
            if (dist <= _enemy.AttackRange)
            {
                return EnemyState.Attack;
            }

            // Otherwise, transition to Chase.
            return EnemyState.Chase;
        }

        // Otherwise, remain in BackDodge state.
        return StateKey;
    }
}
