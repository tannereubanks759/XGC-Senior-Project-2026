/*
 * AttackStateFSM defines the behavior of an enemy while in its Attack state.
 * This state handles enabling the sword collider, rotating toward the player,
 * playing the attack animation, and deciding what state comes next.
 * 
 * By: Matthew Bolger
 */

using UnityEngine;

// AttackStateFSM inherits from BaseState<EnemyState>, meaning it represents
// one specific state (Attack) in the enemy’s finite state machine.
public class AttackStateFSM : BaseState<EnemyState>
{
    // Reference to the enemy AI using this state.
    private BaseEnemyAI _enemy;

    // Attack cooldown and tracking of when the last attack happened.
    private float _attackCooldown = 1.5f;
    private float _lastAttackTime = -Mathf.Infinity;

    // Rotation speed while turning to face the player during an attack.
    private float rotationSpeed = 10f;

    // Collider for the enemy’s sword or weapon.
    private Collider swordCollider;

    // Constructor requires the enum key for this state, 
    // a reference to the enemy AI, and the sword collider.
    public AttackStateFSM(EnemyState key, BaseEnemyAI enemy, Collider sword) : base(key)
    {
        _enemy = enemy;
        swordCollider = sword;
    }

    // Called when the Attack state is first entered.
    public override void EnterState()
    {
        //Debug.Log("Entered Attack State");

        // Enable sword collider so it can hit the player.
        swordCollider.enabled = true;

        // Stop enemy movement and face the player before attacking.
        _enemy.StopMoving();
        _enemy.RotateToPlayer();

        // Reset all animator triggers to prevent conflicts.
        _enemy.ResetTriggers();

        // Trigger the attack animation.
        _enemy.Animator.SetTrigger("Attack");

        // Record the time this attack started for cooldown tracking.
        _lastAttackTime = Time.time;
    }

    // Called once when leaving the Attack state.
    public override void ExitState()
    {
        //Debug.Log("Exiting Attack State");

        // Disable sword collider so it no longer deals damage.
        swordCollider.enabled = false;

        // Reset attack state on the enemy to "None."
        _enemy.SetAttackState(BaseEnemyAI.AttackState.None);

        // Reset animator triggers and resume navigation movement.
        _enemy.ResetTriggers();
        _enemy.ResumeMoving();
    }

    // Called every frame while in the Attack state.
    public override void UpdateState()
    {
        // Smoothly rotate toward the player during the attack if allowed.
        if (_enemy.Player != null && _enemy.canRotate)
        {
            Vector3 direction = (_enemy.Player.position - _enemy.transform.position).normalized;
            direction.y = 0f; // Ignore vertical component to prevent tilting.

            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                _enemy.transform.rotation = Quaternion.Slerp(
                    _enemy.transform.rotation,
                    lookRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        // Check if the attack animation has finished.
        if (_enemy.IsAttackFinished)
        {
            // Reset the attack state so another attack can be triggered later.
            _enemy.SetAttackState(BaseEnemyAI.AttackState.None);

            // If the player is still in range and the cooldown has expired,
            // the enemy could trigger another attack here.
            if (_enemy.DistanceToPlayer() <= _enemy.AttackRange && Time.time - _lastAttackTime >= _attackCooldown)
            {
                // Attack restart logic could be added here if desired.
                _lastAttackTime = Time.time;
            }
        }
    }

    // Determines the next state after Attack.
    public override EnemyState GetNextState()
    {
        // Check first if the enemy is dead.
        if (_enemy.currentHealth <= 0)
            return EnemyState.Dead;

        // Only consider leaving Attack after the attack animation is complete.
        if (_enemy.IsAttackFinished)
        {
            float dist = _enemy.DistanceToPlayer();

            // If the player is out of range, transition to Chase.
            if (dist > _enemy.AttackRange + 0.5f)
                return EnemyState.Chase;

            // Otherwise, randomly decide between staying in Attack,
            // blocking, or performing a back dodge.
            int weight = Random.Range(1, 5);

            switch (weight)
            {
                case 1:
                case 2:
                    return StateKey; // Stay in Attack
                case 3:
                    return EnemyState.Block;
                case 4:
                    return EnemyState.BackDodge;
            }
        }

        // Default: remain in Attack until animation signals it's finished.
        return StateKey;
    }
}
