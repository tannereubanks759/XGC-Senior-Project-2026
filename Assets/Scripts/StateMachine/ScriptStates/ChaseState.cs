using UnityEngine;

public class ChaseState : BaseState<EnemyState>
{
    private BaseEnemyAI _enemy;

    public ChaseState(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    public override void EnterState()
    {
        Debug.Log("Entered Chase State");
        _enemy.Animator.SetTrigger("Chase");
        _enemy.SetSpeed(_enemy.RunSpeed);
    }

    public override void ExitState()
    {
        Debug.Log("Exiting Chase State");
        _enemy.ResetTriggers();
    }

    public override void UpdateState()
    {
        _enemy.MoveTo(_enemy.Player.position);
    }

    public override EnemyState GetNextState()
    {
        float dist = _enemy.DistanceToPlayer();

        if (dist < _enemy.AttackRange)
            return EnemyState.Attack;

        if (dist > _enemy.ChaseRange + 2f) // Add a buffer so they don't constantly flip
            return EnemyState.Idle;

        return StateKey;
    }
}
