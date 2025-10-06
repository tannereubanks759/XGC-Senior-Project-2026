/*
 * IdleState defines the behavior of an enemy while in its Idle state.
 * This state handles the enemy standing still doing nothing, playing
 * the idle animation, and deciding what state comes next.
 * 
 * By: Matthew Bolger
 */
using UnityEngine;

// Represents the Idle state in the enemy's finite state machine.
// In this state, the enemy waits without moving until conditions trigger a transition
public class IdleState : BaseState<EnemyState>
{

    [Header("Idle State")]
    // Reference to the enemy AI using this state.
    private BaseEnemyAI _enemy;

    // Time in idle state
    private float idleTime;

    [Tooltip("The amount of time needed to pass for the enemy to transistion to a new state")]
    [SerializeField]private float changeStateInterval = 5f;

    // Constructor requires the enum key for this state, 
    // and a reference to the enemy AI
    public IdleState(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    //
    public override void EnterState()
    {
        // Stop navmesh movement
        _enemy.StopMoving();

        // Set the animator movement
        _enemy.SetAnimatorMovement(0f, 0f);

        // Set the time we entered the idle state
        idleTime = Time.time;

    }

    // Final function called when transitioning to a new state
    public override void ExitState()
    {

    }

    // Called before update so that the we are in the correct state beofre we do anything
    public override EnemyState GetNextState()
    {
        // The enemy is out of health
        if (_enemy.currentHealth <= 0) return EnemyState.Dead;

        if (_enemy.canSeePlayerNow) return EnemyState.Run;
            
        // Enough time has passed to transition
        if (Time.time -  idleTime >= changeStateInterval)
        {
            changeStateInterval = 5f;

            _enemy.SetResetTriggers("EmoteOver");

            float rand = Random.value;

            float emotePerc = 0.5f / 3;

            if (rand <= 0.5)
            {
                return EnemyState.Patrol;
            }
            else if (rand <= 0.5 + emotePerc)
            {
                _enemy.SetResetTriggers("Emote1");
                idleTime = Time.time;
                return StateKey;
            }
            else if (rand <= 0.5 + (2 * emotePerc))
            {
                _enemy.SetResetTriggers("Emote2");
                idleTime = Time.time;
                changeStateInterval = 7f;
                return StateKey;
            }
            else if (rand <= 0.5 + (3 * emotePerc))
            {
                _enemy.SetResetTriggers("Emote3");
                idleTime = Time.time;
                changeStateInterval = 8f;
                return StateKey;
            }
        }
        return StateKey;
    }

    // Called after GetNextState() if it returns the same state that the enemy is currently in
    public override void UpdateState()
    {
        // Nothing to do yet
    }
}
