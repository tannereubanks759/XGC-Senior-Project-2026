using UnityEngine;

public class BasicAttackState : BaseState<EnemyState>
{
    private BaseEnemyAI _enemy;
    private float attackTime;
    public BasicAttackState(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    public override void EnterState()
    {
        //Debug.Log("Entered Attack");

        _enemy.SetAttackState(BaseEnemyAI.EAttackState.InProgress);

        _enemy.SetResetTriggers("Attack");

        var attackIndex = Random.Range(0, 4);

        _enemy.Animator.SetInteger("AttackIndex", attackIndex);

    }

    public override void ExitState()
    {
        //Debug.Log("Exited Attack");

        _enemy.SetAttackState(BaseEnemyAI.EAttackState.None);
    }

    public override EnemyState GetNextState()
    {
        if (_enemy.currentHealth <= 0) return EnemyState.Dead;

        if (_enemy.CurrentAttackState == BaseEnemyAI.EAttackState.None)
        {
            return EnemyState.Chase;
        }
        return StateKey;
    }

    public override void UpdateState()
    {
        if (_enemy.CurrentAttackState == BaseEnemyAI.EAttackState.Finished && attackTime == 0)
        {
            attackTime = Time.time;
        }

        if (Time.time - attackTime >= .5f && _enemy.CurrentAttackState == BaseEnemyAI.EAttackState.Finished)
        {
            _enemy.SetAttackState(BaseEnemyAI.EAttackState.None);
            attackTime = 0;
        }
    }
}
