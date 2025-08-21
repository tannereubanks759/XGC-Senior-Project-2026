using UnityEngine;
public class PatrolState : BaseState<EnemyState>
{
    private BaseEnemyAI _enemy;
    private Vector3 _currentTarget;
    private PatrolArea _patrolArea;

    public PatrolState(EnemyState key, BaseEnemyAI enemy, PatrolArea area) : base(key)
    {
        _enemy = enemy;
        _patrolArea = area;
    }

    public override void EnterState()
    {
        Debug.Log("Entered Patrol State");
        _enemy.Animator.SetTrigger("Patrol");
        _enemy.SetSpeed(_enemy.WalkSpeed);
        PickNewTarget();
    }

    public override void ExitState()
    {
        Debug.Log("Exiting Patrol");
        _enemy.ResetTriggers();
    }

    public override void UpdateState()
    {
        if (_enemy.Agent.remainingDistance < 0.5f)
        {
            _enemy.TransitionToState(EnemyState.Idle);
            PickNewTarget();
        }

        _enemy.MoveTo(_currentTarget);
    }

    private void PickNewTarget()
    {
        _currentTarget = _patrolArea.GetRandomPoint();
    }

    public override EnemyState GetNextState()
    {
        if (_enemy.currentHealth <= 0)
            return EnemyState.Dead;
        else if (_enemy.DistanceToPlayer() < _enemy.ChaseRange)
            return EnemyState.Chase;

        return StateKey;
    }
}
