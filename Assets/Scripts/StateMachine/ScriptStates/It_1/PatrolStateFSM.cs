/*
 * PatrolStateFSM.cs
 * 
 * This script defines the Patrol state for an enemy in the state machine.
 * When the enemy enters this state, it moves between random points in a defined patrol area.
 * The enemy will remain patrolling until either the player comes within chase range
 * or the enemy dies.
 * 
 * By: Matthew Bolger
*/

using UnityEngine;

// Represents the Patrol state in the enemy's finite state machine.
// In this state, the enemy walks between random points within a patrol area.
public class PatrolStateFSM : BaseState<EnemyState>
{
    // Reference to the enemy this state belongs to.
    private BaseEnemyAI _enemy;

    // The current patrol target position the enemy is moving toward.
    private Vector3 _currentTarget;

    // Reference to the patrol area that provides random points for patrolling.
    private PatrolArea _patrolArea;

    // Constructor — initializes the state with its key, enemy reference, and patrol area.
    public PatrolStateFSM(EnemyState key, BaseEnemyAI enemy, PatrolArea area) : base(key)
    {
        _enemy = enemy;
        _patrolArea = area;
    }

    // Called once when entering the Patrol state.
    // Sets animation, movement speed, and selects the first patrol target.
    public override void EnterState()
    {
        //Debug.Log("Entered Patrol State");

        // Trigger the patrol animation.
        _enemy.Animator.SetTrigger("Patrol");

        // Set enemy speed to walking speed for patrolling.
        _enemy.SetSpeed(_enemy.WalkSpeed);

        // Pick an initial random patrol target.
        PickNewTarget();
    }

    // Called once when leaving the Patrol state.
    // Resets animator triggers to avoid conflicts.
    public override void ExitState()
    {

        _enemy.ResetTriggers();
    }

    // Called every frame while in the Patrol state.
    // Moves toward the current patrol target and checks if a new target is needed.
    public override void UpdateState()
    {
        // If the enemy is close to the target, switch to Idle briefly and pick a new target.
        if (_enemy.Agent.remainingDistance < 0.5f)
        {
            _enemy.TransitionToState(EnemyState.Idle);
            PickNewTarget();
        }

        // Move toward the current patrol target.
        _enemy.MoveTo(_currentTarget);
    }

    // Selects a new random target within the patrol area.
    private void PickNewTarget()
    {
        _currentTarget = _patrolArea.GetRandomPoint(10);
        //Debug.Log("New Target: " + _currentTarget);
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

        // Otherwise, remain in Patrol state.
        return StateKey;
    }
}
