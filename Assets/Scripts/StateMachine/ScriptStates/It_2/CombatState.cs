/*
 * CombatState defines the state where the enemy will handle combat.
 * The enemy will strafe around, walk towards, dodge, block, and attack the
 * player.
 * 
 * By: Matthew Bolger
 */
using UnityEngine;

public class CombatState : BaseState<EnemyState>
{
    // reference to the enemy
    private BaseEnemyAI _enemy;

    //
    public CombatState(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    //
    public override void EnterState()
    {
        throw new System.NotImplementedException();
    }

    //
    public override void ExitState()
    {
        throw new System.NotImplementedException();
    }

    //
    public override EnemyState GetNextState()
    {
        // If dead
        if (_enemy.currentHealth <= 0)
            return EnemyState.Dead;

        // If we can’t see the player anymore
        if (!_enemy.canSeePlayerNow)
            return EnemyState.Investigate;

        // If player is outside combat radius, go back to chase/run
        if (_enemy.DistanceToPlayer() > _enemy.ChaseRange)
            return EnemyState.Run;

        // If player is within attack range maybe attack
        if (_enemy.DistanceToPlayer() <= _enemy.AttackRange)
        {
            // Example simple choice
            float roll = Random.value;
            if (roll < 0.6f) return EnemyState.Attack;
            if (roll < 0.8f) return EnemyState.Block;
            return EnemyState.BackDodge;
        }

        // Otherwise stay in combat
        return StateKey;
    }

    //
    public override void UpdateState()
    {
        _enemy.RotateToPlayer();

        float distance = _enemy.DistanceToPlayer();

        if (distance > _enemy.AttackRange)
        {
            // Walk toward player
            Vector3 move = _enemy.transform.forward * _enemy.WalkSpeed * Time.deltaTime;
            _enemy.DirectMove(move);
            _enemy.SetAnimatorMovement(0f, 1f); // zMov forward
        }
        else
        {
            // Strafe inside combat range
            float strafeDir = Random.value < 0.5f ? -1f : 1f;
            Vector3 move = _enemy.transform.right * strafeDir * _enemy.WalkSpeed * Time.deltaTime;
            _enemy.DirectMove(move);
            _enemy.SetAnimatorMovement(strafeDir, 0f); // xMov left/right
        }
    }
}
