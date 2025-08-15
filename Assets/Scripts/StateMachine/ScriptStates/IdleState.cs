using UnityEngine;

public class IdleState : BaseState<EnemyState>
{
    private BaseEnemyAI _enemy;

    private float idleTime;
    private float timeBeforPatrol = 5f;

    public IdleState(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    public override void EnterState()
    {
        Debug.Log("Entered Idle State");
        _enemy.Animator.SetTrigger("Idle");
        _enemy.StopMoving();
        idleTime = 0f;
    }

    public override void ExitState()
    {
        Debug.Log("Exiting Idle State");
        _enemy.ResumeMoving();
        _enemy.ResetTriggers();
    }

    public override void UpdateState()
    {
        idleTime += Time.deltaTime;
    }

    public override EnemyState GetNextState()
    {
        if (_enemy.currentHealth <= 0)
            return EnemyState.Dead;
        else if (_enemy.DistanceToPlayer() < _enemy.ChaseRange)
            return EnemyState.Chase;
        else if (idleTime >= timeBeforPatrol)
            return EnemyState.Patrol;
        return StateKey;
    }
}
