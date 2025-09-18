/*
 * IdleStateFSM.cs
 * 
 * This script defines the Idle state for an enemy in the state machine.
 * When the enemy enters this state, it stops moving and plays its idle animation.
 * The enemy will remain idle until either the player comes within chase range,
 * a patrol timer finishes, or the enemy dies.
 * 
 * By: Matthew Bolger
*/

using UnityEngine;

// Represents the Idle state in the enemy's finite state machine.
// In this state, the enemy waits without moving until conditions trigger a transition.
public class IdleStateFSM : BaseState<EnemyState>
{
    // Reference to the enemy this state belongs to.
    private BaseEnemyAI _enemy;

    // Tracks how long the enemy has been idle.
    private float idleTime;

    // Time before the enemy transitions into a patrol if no other condition is met.
    private float timeBeforPatrol = 5f;

    // Constructor — initializes the state with its key and a reference to the enemy AI.
    public IdleStateFSM(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    // Called once when entering the Idle state.
    // Sets the enemy to idle animation and stops its movement.
    public override void EnterState()
    {
        Debug.Log("Entered Idle State");

        // Trigger the idle animation.
        _enemy.Animator.SetTrigger("Idle");

        // Ensure the enemy is not moving.
        _enemy.StopMoving();

        // Reset idle time 
        idleTime = Time.time;
    }

    // Called once when leaving the Idle state.
    // Resumes enemy movement and resets animator triggers.
    public override void ExitState()
    {
        Debug.Log("Exiting Idle State");

        // Allow the enemy to move again (for patrol or chase).
        _enemy.ResumeMoving();

        // Reset animator triggers to avoid animation conflicts.
        _enemy.ResetTriggers();
    }

    // Called every frame while in the Idle state.
    public override void UpdateState()
    {
        // Nothing to do
    }

    // Determines the next state based on current conditions.
    public override EnemyState GetNextState()
    {
        // If the enemy has no health left, transition to Dead.
        if (_enemy.currentHealth <= 0)
            return EnemyState.Dead;

        // If the player is close enough, transition to Chase.
        else if (_enemy.DistanceToPlayer() < _enemy.ChaseRange)
            return EnemyState.Chase;

        // If idle time has passed the patrol threshold, transition to Patrol.
        else if ((Time.time - idleTime) >= timeBeforPatrol)
            return EnemyState.Patrol;

        // Otherwise, remain in Idle state.
        return StateKey;
    }
}
