/*
 * PatrolState.cs
 * 
 * This script defines the Patrol state for an enemy in the state machine.
 * When the enemy enters this state, it moves between random points in a defined patrol area.
 * 
 * By: Matthew Bolger
*/
using UnityEngine;

public class PatrolState : BaseState<EnemyState>
{
    // Reference to the enemy AI using this state.
    private BaseEnemyAI _enemy;

    // Reference to the patrol area this enemy uses to find a partol point
    private PatrolArea _area;

    // The current target
    private Vector3 _currentTarget;
    private float _reachedThreshold; // how close is "at target?"
    private Vector3 currentSpeed;

    public PatrolState(EnemyState key, BaseEnemyAI enemy, PatrolArea area) : base(key)
    {
        _enemy = enemy;
        _area = area;
        _reachedThreshold = _enemy.AttackRange - 0.5f;
    }

    public override void EnterState()
    {
        currentSpeed = new Vector3(0.0f, 0.0f, 1.0f);

        _enemy.SetAnimatorMovement(0f, 1f);

        _enemy.SetSpeed(_enemy.WalkSpeed);

        _currentTarget = _area.GetRandomPoint();

        _enemy.MoveTo(_currentTarget);
    }

    public override void ExitState()
    {
        _enemy.StopMoving();
    }

    public override EnemyState GetNextState()
    {
        // Dead check
        if (_enemy.currentHealth <= 0) return EnemyState.Dead;

        // If we can see the player
        if (_enemy.canSeePlayerNow) return EnemyState.Run;

        // Reached the patrol target
        if (!_enemy.Agent.pathPending && _enemy.Agent.remainingDistance <= _reachedThreshold)
        {
            return EnemyState.Idle; // pause/idling between patrol points
        }

        // Stay in patrol
        return StateKey;
    }

    public override void UpdateState()
    {
        if (_enemy.Agent.remainingDistance <= _reachedThreshold + 2.0f && currentSpeed.z > 0f)
        {
            currentSpeed.z -= 0.6f * Time.deltaTime;

            _enemy.SetSpeed(_enemy.CurrentSpeed - Time.deltaTime);

            _enemy.SetAnimatorMovement(currentSpeed.x, currentSpeed.z);
        }
    }
}
