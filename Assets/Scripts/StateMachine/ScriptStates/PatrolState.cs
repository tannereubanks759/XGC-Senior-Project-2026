using UnityEngine;
public class PatrolState : BaseState<EnemyState>
{
    private BaseEnemyAI _enemy;
    private int _currentWaypointIndex = 0;

    public PatrolState(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    public override void EnterState()
    {
        Debug.Log("Entered Patrol State");
        _enemy.MoveTo(_enemy.PatrolPoints[_currentWaypointIndex].transform.position);
    }

    public override void ExitState() => Debug.Log("Exiting Patrol");

    public override void UpdateState()
    {
        if (_enemy.Agent.remainingDistance < 0.5f)
        {
            _currentWaypointIndex = (_currentWaypointIndex + 1) % _enemy.PatrolPoints.Length;
            _enemy.MoveTo(_enemy.PatrolPoints[_currentWaypointIndex].transform.position);
        }
    }

    public override EnemyState GetNextState()
    {
        if (_enemy.DistanceToPlayer() < _enemy.ChaseRange)
            return EnemyState.Chase;

        return StateKey;
    }
}
