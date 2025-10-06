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

    private float moveTimer = 0f;
    private bool forward = true;

    // These hold animator movement inputs
    private float currentX = 0f;
    private float currentZ = 0f;

    private float dodgeMult = 2.5f;
    private float dodgeTimer = 0f;
    private float dodgeTransition = 0.75f;

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

        currentX = 0f;
        currentZ = 0f;
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
        //if (!_enemy.canSeePlayerNow)
            // return EnemyState.Investigate;

        if (!_enemy.isDodging)
        {
            // If player is outside combat radius, go back to chase/run
            if (_enemy.DistanceToPlayer() > _enemy.combatRange + 2f)
                return EnemyState.Run;

            // If player is within attack range maybe attack
            if (_enemy.DistanceToPlayer() <= _enemy.AttackRange)
            {
                // Example simple choice
                float roll = Random.value;
                if (roll < 0.6f)
                {
                    return EnemyState.Attack;
                }

                if (roll < 0.8f)
                {
                    _enemy.isBlocking = true;
                    _enemy.SetResetTriggers("Block");
                }

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

        if (!_enemy.isDodging && !_enemy.isBlocking)
        {
            // --- PLAYER ATTACK REACTION ---
            if (_enemy.PlayerIsAttacking() && distance <= _enemy.threatRange)
            {
                float dodgeOrBlock = Random.value;
                if (dodgeOrBlock < 0.2f)
                {
                    _enemy.isDodging = true;
                    _enemy.SetResetTriggers("BackDodge");
                }
                else
                {
                    // Block
                    _enemy.isBlocking = true;
                    _enemy.SetResetTriggers("Block");
                }
                return; // skip other movement this frame
            }

            moveTimer -= Time.deltaTime;
            if (moveTimer <= 0)
            {
                if (currentX != 0 || currentZ != 0)
                {
                    currentX = Mathf.Lerp(currentX, 0, 1f);
                    currentZ = Mathf.Lerp(currentZ, 0, 1f);
                }

                float roll = Random.value;

                forward = roll < .8 ? true : false;

                if (!forward)
                {
                    strafeDirection = Random.value < 0.5f ? -1f : 1f;
                }

                currentX = 0.0f;
                currentZ = 0.0f;

                moveTimer = Random.Range(1.0f, 3.0f);
            }

            if (forward)
            {
                if (currentZ < _enemy.WalkSpeed)
                {
                    currentZ = Mathf.Lerp(currentZ, _enemy.WalkSpeed, 1f);
                }
                Vector3 forwardMove = (_enemy.Player.position - _enemy.transform.position).normalized;
                _enemy.DirectMove(forwardMove * currentZ * Time.deltaTime);
                _enemy.SetAnimatorMovement(0f, 1f);
                return;
            }

            if (currentX < _enemy.combatSpeed)
            {
                currentX = Mathf.Lerp(currentX, _enemy.combatSpeed, 1f);
            }

            // --- ORBIT / STRAFE AROUND PLAYER ---
            Vector3 strafe = _enemy.transform.right * strafeDirection;
            Vector3 toPlayer = (_enemy.Player.position - _enemy.transform.position).normalized;

            // Combine strafe + small forward movement to orbit naturally
            Vector3 move = (strafe + toPlayer * 0.2f).normalized * currentX * Time.deltaTime;
            _enemy.DirectMove(move);
            _enemy.SetAnimatorMovement(strafeDirection, 0.2f); // x/z movement for animator
        }
        if (_enemy.isDodging)
        {
            Vector3 awayFromPlayer = (_enemy.transform.position - _enemy.Player.position).normalized;
            Vector3 move = awayFromPlayer * Time.deltaTime * dodgeMult;
            _enemy.DirectMove(move);
        }
    }
}
