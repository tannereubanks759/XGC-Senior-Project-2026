using UnityEngine;

public class IdleState : BaseState<EnemyState>
{
    private BaseEnemyAI _enemy;

    public IdleState(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    public override void EnterState()
    {
        Debug.Log("Entered Idle State");
        _enemy.Animator.SetTrigger("Idle");
        _enemy.StopMoving();
    }

    public override void ExitState()
    {
        Debug.Log("Exiting Idle State");
        _enemy.ResetTriggers();
    }

    public override void UpdateState()
    {
        // Optional idle behavior (e.g. look around, animation)
    }

    public override EnemyState GetNextState()
    {
        if (_enemy.DistanceToPlayer() < _enemy.ChaseRange)
        {
            return EnemyState.Chase;
        }

        return StateKey;
    }
}
