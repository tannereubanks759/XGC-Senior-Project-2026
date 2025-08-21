using UnityEngine;

public class AttackState : BaseState<EnemyState>
{
    private BaseEnemyAI _enemy;
    private float _attackCooldown = 1.5f;
    private float _lastAttackTime = -Mathf.Infinity;

    public AttackState(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    public override void EnterState()
    {
        Debug.Log("Entered Attack State");
        _enemy.Animator.SetTrigger("Attack");
        _enemy.StopMoving();
        _enemy.RotateToPlayer();
    }

    public override void ExitState()
    {
        Debug.Log("Exiting Attack State");
        _enemy.ResetTriggers();
        _enemy.ResumeMoving();
    }

    public override void UpdateState()
    {
        if (Time.time - _lastAttackTime >= _attackCooldown)
        {
            _enemy.Attack();
            _lastAttackTime = Time.time;
        }
    }

    public override EnemyState GetNextState()
    {
        float dist = _enemy.DistanceToPlayer();

        if (_enemy.currentHealth <= 0)
            return EnemyState.Dead;
        if (dist > _enemy.AttackRange)
            return EnemyState.Chase;
        return StateKey;
    }
}
