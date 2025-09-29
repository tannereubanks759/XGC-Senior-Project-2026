/*
 * RunState defines the behavior of an enemy while in its Run state.
 * This state handles the enemy running after the player
 * 
 * By: Matthew Bolger
 */
using UnityEngine;

public class RunState : BaseState<EnemyState>
{
    // Reference to the enemy AI using this state.
    private BaseEnemyAI _enemy;
    private bool isRunning;

    public RunState(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    public override void EnterState()
    {
        _enemy.StopMoving();

        _enemy.SetResetTriggers("Warcry");

        isRunning = false;
        _enemy.canRunAtPlayer = false;
    }

    public override void ExitState()
    {
        
    }

    public override EnemyState GetNextState()
    {
        if (isRunning && _enemy.DistanceToPlayer() <= _enemy.combatRange) return EnemyState.Combat;

        return StateKey;
    }

    public override void UpdateState()
    {
        if (_enemy.canRunAtPlayer && !isRunning)
        {
            isRunning = true;
            _enemy.SetResetTriggers("Run");
            _enemy.ResumeMoving();
            _enemy.SetSpeed(_enemy.RunSpeed);
        }

        if (isRunning)
        {
            // Continuously move toward player
            _enemy.RunTowardsPlayer();
        }
    }
}
