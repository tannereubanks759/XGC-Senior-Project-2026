/*
 * BasicPatrolState.cs
 * 
 * This state handles the enemy patroling from patrol point
 * to patrol point. The enemy slowly speeds up when starting 
 * its path and slowly slows down the enemy when it approaches
 * its destination. 
 * 
 * State Transitions:
 * If the enemy's health drops below 0, they die.
 * If the enemy can see the player, they chase.
 * If the enemy has reached its destination, they idle;
 * 
 * By: Matthew Bolger
*/
using UnityEngine;

public class BasicPatrolState : BaseState<EnemyState>
{
    // References
    private BaseEnemyAI _enemy;
    private PatrolArea _area;

    // Local Variables
    private bool destinationReached;

    // Constructor
    public BasicPatrolState(EnemyState key, BaseEnemyAI enemy, PatrolArea area) : base(key)
    {
        _enemy = enemy;
        _area = area;
    }

    public override void EnterState()
    {
        // Reset destination flag
        destinationReached = false;
        
        // Get the enemy's next destination
        _enemy.Agent.destination = _area.GetRandomPoint(10);
    }

    public override void ExitState()
    {
        // Optional cleanup or transition effects (currently unused)
    }

    public override EnemyState GetNextState()
    {
        // The enemy's health drops below 0
        if (_enemy.currentHealth <= 0) return EnemyState.Dead;

        // The enemy can see the player
        if (_enemy.canSeePlayerNow)
        {
            _area.PlayerSeen();
            return EnemyState.Chase;
        }

        // The enemy has reached its destination
        if (destinationReached) return EnemyState.Idle;

        // Stay in patrol
        return StateKey;
    }

    public override void UpdateState()
    {
        // Return early if the agent is disabled
        if (!_enemy.Agent.enabled) return;

        // Only move when the agent has a path to follow
        if (_enemy.Agent.hasPath)
        {
            // Calculate direction to next steering target
            var dir = (_enemy.Agent.steeringTarget - _enemy.transform.position).normalized;
            
            // Convert movement direction into local space for anim blending
            var animdir = _enemy.transform.InverseTransformDirection(dir);
            
            // Check if the enemy is facing the movement direction
            var isFacingMoveDirection = Vector3.Dot(dir, _enemy.transform.forward) > .25f;

            // Smoothly rotate towards the movement direction
            _enemy.transform.rotation = Quaternion.RotateTowards(_enemy.transform.rotation, Quaternion.LookRotation(dir), 360 * Time.deltaTime);

            // Adjust to account for enemy offset
            float distance = _enemy.Agent.remainingDistance - .875f;
            
            // The radius in which the enemy will begin to slow down
            float slowDownRadius = 2.75f;

            // Normalized slowdown factor (0-1)
            float distanceFactor = Mathf.Clamp01(distance / slowDownRadius);

            // Determine the base speed depending on health
            float baseSpeed = (_enemy.currentHealth < 15) ? _enemy.damagedSpeed : _enemy.maxSpeed;
            
            // Final speed scales with how close we are to the destination
            float targetSpeed = baseSpeed * distanceFactor;

            // The enemy is facing the direction of movement
            if (isFacingMoveDirection)
            {
                _enemy.Animator.SetFloat("Speed", Mathf.Lerp(0f, targetSpeed, animdir.z), .75f, Time.deltaTime);
                _enemy.Agent.speed = targetSpeed;
            }

            // Not facing direction of movement, so stop
            else
            {
                _enemy.Animator.SetFloat("Speed", 0f, .75f, Time.deltaTime);
                _enemy.Agent.speed = 0f;
            }

            // Mark patrol as complete and clear the path when the destination is reached
            if (Vector3.Distance(_enemy.transform.position, _enemy.Agent.destination) < _enemy.Agent.radius)
            {
                destinationReached = true;
                _enemy.Agent.ResetPath();
            }
        }
        else
        {
            // Stop the animation
            _enemy.Animator.SetFloat("Speed", 0f, .5f, Time.deltaTime);
        }
    }
}
