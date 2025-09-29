/*
 * HitStateFSM.cs
 * 
 * This script defines the Hit (flinch) state for an enemy in the state machine.
 * When the enemy enters this state, it plays a hit reaction animation, pauses movement,
 * and temporarily prevents attacking until the animation duration has passed.
 * After the hit duration, the enemy transitions to another state depending on conditions.
 * 
 * By: Matthew Bolger
*/

using UnityEngine;

// Represents the Hit (flinch) state in the enemy's finite state machine.
// In this state, the enemy reacts to being damaged by stopping movement and playing a hit animation.
public class HitStateFSM : BaseState<EnemyState>
{
    // Reference to the enemy this state belongs to.
    private BaseEnemyAI _enemy;

    // Duration (in seconds) that the hit reaction lasts.
    private float hitDuration = 1f;

    // The time when the enemy entered the Hit state (used to track animation duration).
    private float enterTime;

    // Constructor — initializes the state with its key and a reference to the enemy AI.
    public HitStateFSM(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    // Called once when entering the Hit state.
    // Handles stopping movement, resetting attack states, and triggering the hit animation.
    public override void EnterState()
    {
        //Debug.Log("Entered Hit State");

        // Stop movement so the enemy pauses during the hit reaction.
        _enemy.StopMoving();

        // Reset attack state so the enemy can’t attack while flinching.
        _enemy.SetAttackState(BaseEnemyAI.EAttackState.None);

        // Reset animation triggers to avoid conflicts with other animations.
        _enemy.ResetTriggers();

        // Play the "Hit" animation.
        _enemy.Animator.SetTrigger("Hit");

        // Record the time the state was entered (for timing the hit duration).
        enterTime = Time.time;
    }

    // Called once when leaving the Hit state.
    // Ensures the enemy can move again and resets animation triggers.
    public override void ExitState()
    {
        //Debug.Log("Exiting Hit State");

        // Reset any lingering animation triggers.
        _enemy.ResetTriggers();

        // Allow the enemy to resume moving.
        _enemy.ResumeMoving();
    }

    // Called every frame while in the Hit state.
    // In this case, no active updates are needed — we only wait for the hit duration to finish.
    public override void UpdateState()
    {
        // Wait for the hit animation duration before transitioning.
    }

    // Determines the next state after the Hit reaction finishes.
    public override EnemyState GetNextState()
    {
        // If the enemy has no health left, transition to Dead state.
        if (_enemy.currentHealth <= 0)
            return EnemyState.Dead;

        // After the hit animation duration has passed...
        if (Time.time - enterTime >= hitDuration)
        {
            float dist = _enemy.DistanceToPlayer();

            // If player is in attack range, transition to Attack.
            if (dist <= _enemy.AttackRange)
            {
                return EnemyState.Attack;
            }

            // If player is within chase range, transition to Chase.
            if (dist <= _enemy.ChaseRange)
            {
                return EnemyState.Chase;
            }

            // Otherwise, transition back to Idle.
            return EnemyState.Idle;
        }

        // If still within hit duration, remain in Hit state.
        return StateKey;
    }
}
