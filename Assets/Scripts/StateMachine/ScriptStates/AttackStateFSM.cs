using UnityEngine;

public class AttackStateFSM : BaseState<EnemyState>
{
    private BaseEnemyAI _enemy;
    private float _attackCooldown = 1.5f;
    private float _lastAttackTime = -Mathf.Infinity;

    private float rotationSpeed = 10f;

    public AttackStateFSM(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    public override void EnterState()
    {
        Debug.Log("Entered Attack State");

        // Stop movement and face player
        _enemy.StopMoving();
        _enemy.RotateToPlayer();

        // Reset all triggers
        _enemy.ResetTriggers();

        // Trigger anim
        _enemy.Animator.SetTrigger("Attack");

        // Record last attack time
        _lastAttackTime = Time.time;
    }

    public override void ExitState()
    {
        Debug.Log("Exiting Attack State");

        // Reset attack state
        _enemy.SetAttackState(BaseEnemyAI.AttackState.None);

        // Reset animator triggers and resume movement
        _enemy.ResetTriggers();
        _enemy.ResumeMoving();
    }

    public override void UpdateState()
    {
        // Smoothly rotate toward the player while attacking
        if (_enemy.Player != null && _enemy.canRotate)
        {
            Vector3 direction = (_enemy.Player.position - _enemy.transform.position).normalized;
            direction.y = 0f; // ignore vertical rotation
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                _enemy.transform.rotation = Quaternion.Slerp(_enemy.transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
            }
        }

        // Check if attack animation finished (via anim event or AnimatorState info)
        if (_enemy.IsAttackFinished)
        {
            // Reset attack state for next swing
            _enemy.SetAttackState(BaseEnemyAI.AttackState.None);

            // If player still in range and cooldown passed, trigger attack again
            if (_enemy.DistanceToPlayer() <= _enemy.AttackRange && Time.time - _lastAttackTime >= _attackCooldown)
            {
                //_enemy.ResetTriggers();
                //_enemy.Animator.SetTrigger("Attack");
                //_enemy.SetAttackState(BaseEnemyAI.AttackState.InProgress);
                _lastAttackTime = Time.time;
            }
        }
    }

    public override EnemyState GetNextState()
    {
        // Always check if the enemy is dead first
        if (_enemy.currentHealth <= 0)
            return EnemyState.Dead;

        // Only evaluate leaving Attack state after the animation finishes
        if (_enemy.IsAttackFinished)
        {
            float dist = _enemy.DistanceToPlayer();

            // If player is out of attack range, transition to Chase
            if (dist > _enemy.AttackRange + 0.5f)
                return EnemyState.Chase;

            // Optionally, if player is still in range, stay in Attack
            return StateKey; // keeps enemy in Attack state
        }

        // Default: stay in the current Attack state until animation signals finished
        return StateKey;
    }
}
