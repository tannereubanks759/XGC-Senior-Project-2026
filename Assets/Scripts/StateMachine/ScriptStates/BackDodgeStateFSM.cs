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

    // Time we entered into the backdodge & its length
    private float enterTime;
    private float backDodgeDuration = 1f; 

    // Distance the backdode travels
    private float backDodgeDistance = 4f;

    // Speed based on distance / duration;
    private float backDodgeSpeed;

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
        _enemy.AgentUpdateOff();
        _enemy.RotateToPlayer();

        // Reset all animation triggers to avoid conflicts.
        _enemy.ResetTriggers();

        // Mark the enemy as currently dodging.
        _enemy.isDodging = true;

        // Trigger the back-dodge animation.
        _enemy.Animator.SetTrigger("BackDodge");

        // Calc the speed
        backDodgeSpeed = backDodgeDistance / backDodgeDuration;
    }

    // Called once when leaving the BackDodge state.
    // Resets animation triggers and allows the enemy to resume normal movement.
    public override void ExitState()
    {
        _enemy.ResetTriggers();
        _enemy.AgentUpdateOn();
        _enemy.isDodging = false;
    }

    // Called every frame while in the BackDodge state.
    public override void UpdateState()
    {
        float elapsed = Time.time - enterTime;
        if (elapsed < backDodgeDuration)
        {
            Vector3 stepBack = -_enemy.transform.forward * backDodgeSpeed * Time.deltaTime;
            _enemy.transform.position += stepBack;
        }
    }

    // Determines the next state after the dodge is complete.
    public override EnemyState GetNextState()
    {
        // If the dodge is finished (set in anim event)
        if (!_enemy.isDodging)
        {
            // The distance to the player
            float dist = _enemy.DistanceToPlayer();

            // The enemy is dead
            if (_enemy.currentHealth <= 0)
                return EnemyState.Dead;
            // Player in attack range
            else if (dist <= _enemy.AttackRange)
                return EnemyState.Attack;
            // Player in chase range
            else if (dist <= _enemy.ChaseRange)
                return EnemyState.Chase;

            // Idle if none
            return EnemyState.Idle;
        }

        // Stay in the dodge state
        return StateKey;
    }
}