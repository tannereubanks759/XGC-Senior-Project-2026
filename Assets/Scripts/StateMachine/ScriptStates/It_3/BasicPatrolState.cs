using UnityEngine;

public class BasicPatrolState : BaseState<EnemyState>
{
    private BaseEnemyAI _enemy;
    private PatrolArea _area;
    private bool destinationReached;
    public BasicPatrolState(EnemyState key, BaseEnemyAI enemy, PatrolArea area) : base(key)
    {
        _enemy = enemy;
        _area = area;
    }

    public override void EnterState()
    {
        destinationReached = false;
        
        _enemy.Agent.destination = _area.GetRandomPoint(10);

        //Debug.Log("Entered Patrol");
    }

    public override void ExitState()
    {
        //Debug.Log("Exited Patrol");
    }

    public override EnemyState GetNextState()
    {
        if (_enemy.currentHealth <= 0) return EnemyState.Dead;

        if (_enemy.canSeePlayerNow) return EnemyState.Chase;

        if (destinationReached) return EnemyState.Idle;

        return StateKey;
    }

    public override void UpdateState()
    {
        if (_enemy.Agent.hasPath)
        {
            var dir = (_enemy.Agent.steeringTarget - _enemy.transform.position).normalized;
            var animdir = _enemy.transform.InverseTransformDirection(dir);
            var isFacingMoveDirection = Vector3.Dot(dir, _enemy.transform.forward) > .5f;

            _enemy.transform.rotation = Quaternion.RotateTowards(_enemy.transform.rotation, Quaternion.LookRotation(dir), 180 * Time.deltaTime);

            _enemy.Animator.SetFloat("Speed", isFacingMoveDirection ? animdir.z : Mathf.Floor(0), .75f, Time.deltaTime);

            if (Vector3.Distance(_enemy.transform.position, _enemy.Agent.destination) < _enemy.Agent.radius)
            {
                _enemy.Agent.ResetPath();
            }
        }
        else
        {
            _enemy.Animator.SetFloat("Speed", Mathf.Floor(0), .5f, Time.deltaTime);
        }

        if (_enemy.Agent.remainingDistance <= _enemy.Agent.radius)
        {
            destinationReached = true;
        }
    }
}
