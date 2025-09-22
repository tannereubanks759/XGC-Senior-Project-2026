/*
 * BlockHitStateFSM.cs
 * 
 * This script defines the Hit (flinch) state for an enemy in the state machine
 * when the enemy enters is blocking. It plays a hit reaction animation.
 * After the hit duration, the enemy transitions to another state depending on conditions.
 * 
 * By: Matthew Bolger
*/

using UnityEngine;

// Represents the Hit (flinch) state during blocking in the enemy's finite state machine.
// In this state the enemy reacts to being hit while blocking
public class BlockHitStateFSM : BaseState<EnemyState>
{
    // Reference to the enemy this state belongs to.
    private BaseEnemyAI _enemy;

    // The time when the enemy entered the Hit state (used to track animation duration).
    private float enterTime;

    // Duration (in seconds) that the hit reaction lasts.
    private float hitDuration = 1f;

    public BlockHitStateFSM(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    public override void EnterState()
    {
        //Debug.Log("Entered Blocking Hit State");

        // Reset animation triggers to avoid conflicts with other animations.
        _enemy.ResetTriggers();

        // Play the "Hit" animation.
        _enemy.Animator.SetTrigger("BlockHit");

        // Record the time the state was entered (for timing the hit duration).
        enterTime = Time.time;
    }

    public override void ExitState()
    {
        //Debug.Log("Exiting Blocking Hit State");

        // Reset animation triggers to avoid conflicts with other animations.
        _enemy.ResetTriggers();
    }
    public override void UpdateState()
    {
        // Wait for the anim to finish
    }

    public override EnemyState GetNextState()
    {
        if (_enemy.currentHealth <= 0)
            return EnemyState.Dead;

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
        }

        return StateKey;
    }
}
