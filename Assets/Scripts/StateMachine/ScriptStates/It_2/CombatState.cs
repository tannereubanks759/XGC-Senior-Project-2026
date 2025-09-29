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

    private float strafeDirection = 1f; // -1 = left, 1 = right
    private float strafeTimer = 0f;

    //
    public CombatState(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    //
    public override void EnterState()
    {
        // Change the FOV so that the player isn't lost as easily during combat
        _enemy.fieldOfView = 360f;

        _enemy.Agent.isStopped = true;
        _enemy.Agent.ResetPath();

        // Enter the first state of combat which is walking forward towards the player
        _enemy.SetResetTriggers("Combat");
    }

    //
    public override void ExitState()
    {
        // Reset the fov
        _enemy.fieldOfView = 90f;

        _enemy.Agent.isStopped = false;
    }

    //
    public override EnemyState GetNextState()
    {
        // If dead
        if (_enemy.currentHealth <= 0)
            return EnemyState.Dead;

        // If we can’t see the player anymore
        if (!_enemy.canSeePlayerNow)
            // return EnemyState.Investigate;

        // If player is outside combat radius, go back to chase/run
        if (_enemy.DistanceToPlayer() > _enemy.combatRange + 2f)
            return EnemyState.Run;

        if (Time.time - _enemy.attackTime >= _enemy.attackCooldown)
        {
            // If player is within attack range maybe attack
            if (_enemy.DistanceToPlayer() <= _enemy.AttackRange)
            {
                // Example simple choice
                float roll = Random.value;
                if (roll < 0.6f) return EnemyState.Attack;
                //if (roll < 0.8f) return EnemyState.Block;
                //return EnemyState.BackDodge;

                return EnemyState.Attack;
            }

            // Random chance to do the long range attack
            Debug.Log("Override before: " + _enemy.overrideAttack);
            float rollLongRange = Random.value;
            if (rollLongRange < 0.1f && _enemy.overrideAttack == false)
            {
                _enemy.overrideAttack = true;
                return EnemyState.Attack;
            }
        }

        // Otherwise stay in combat
        return StateKey;
    }

    //
    public override void UpdateState()
    {
        _enemy.RotateToPlayer();

        float distance = _enemy.DistanceToPlayer();

        // --- PLAYER ATTACK REACTION ---
        if (_enemy.PlayerIsAttacking() && distance <= _enemy.threatRange)
        {
            float dodgeOrBlock = Random.value;
            if (dodgeOrBlock < 0.5f)
            {
                // Back dodge
                //_enemy.SetResetTriggers("BackDodge");
            }
            else
            {
                // Block
                //_enemy.SetResetTriggers("Block");
            }
            //return; // skip other movement this frame
        }

        // --- MOVE TOWARDS PLAYER IF TOO FAR ---
        if (distance > _enemy.AttackRange)
        {
            Vector3 forwardMove = (_enemy.Player.position - _enemy.transform.position).normalized;
            _enemy.DirectMove(forwardMove * _enemy.WalkSpeed * Time.deltaTime);
            _enemy.SetAnimatorMovement(0f, 1f);
            return;
        }

        // --- ORBIT / STRAFE AROUND PLAYER ---
        strafeTimer -= Time.deltaTime;
        if (strafeTimer <= 0f)
        {
            strafeDirection = Random.value < 0.5f ? -1f : 1f;
            strafeTimer = Random.Range(1.0f, 3.0f); // strafe for 1-3 seconds
        }

        Vector3 strafe = _enemy.transform.right * strafeDirection;
        Vector3 toPlayer = (_enemy.Player.position - _enemy.transform.position).normalized;

        // Combine strafe + small forward movement to orbit naturally
        Vector3 move = (strafe + toPlayer * 0.2f).normalized * _enemy.combatSpeed * Time.deltaTime;
        _enemy.DirectMove(move);
        _enemy.SetAnimatorMovement(strafeDirection, 0.2f); // x/z movement for animator
    }
}
