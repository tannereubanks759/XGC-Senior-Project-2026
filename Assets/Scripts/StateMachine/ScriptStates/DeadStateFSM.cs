/*
 * DeadStateFSM.cs
 * 
 * This script defines the Dead state for an enemy in the state machine.
 * When the enemy enters this state, it stops moving, disables AI components,
 * and ensures the enemy remains inactive (stays dead).
 * 
 * By: Matthew Bolger
*/

using UnityEngine;

// Represents the Dead state in the enemy's finite state machine.
// In this state, the enemy is considered defeated and no longer performs any actions.
public class DeadStateFSM : BaseState<EnemyState>
{
    // Reference to the enemy this state belongs to.
    private BaseEnemyAI _enemy;

    // Optional key item or object that could be dropped when the enemy dies.
    private GameObject _key;

    // Constructor — initializes the state with its key and a reference to the enemy AI.
    public DeadStateFSM(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    // Called once when entering the Dead state.
    // Handles animation, movement stop, and disabling components like the NavMeshAgent or collider.
    public override void EnterState()
    {
        Debug.Log($"{_enemy.name} entered DEAD state.");

        // Trigger the "Dead" animation.
        _enemy.Animator.SetTrigger("Dead");

        // Stop any movement or navigation.
        _enemy.StopMoving();

        // Optionally disable pathfinding if NavMeshAgent is used.
        if (_enemy.Agent != null)
        {
            _enemy.Agent.enabled = false;
        }

        // Optionally disable the collider so the enemy can't interact with the player/world anymore.
        Collider col = _enemy.GetComponent<Collider>();
        if (col != null)
            col.enabled = false;
    }

    // Called once when leaving the Dead state.
    // For this state, nothing happens since enemies do not come back to life here.
    public override void ExitState()
    {
        // Nothing to do — dead enemies don't come back (in this case).
    }

    // Called every frame while in the Dead state.
    // Dead enemies have no active behavior to update.
    public override void UpdateState()
    {
        // Dead enemies don't update behavior.
    }

    // Determines the next state to transition to.
    // Dead enemies remain in this state indefinitely.
    public override EnemyState GetNextState()
    {
        // Always remain in Dead state.
        return StateKey;
    }
}
