using UnityEngine;

public class BasicChaseState : BaseState<EnemyState>
{
    private BaseEnemyAI _enemy;

    public BasicChaseState(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    public override void EnterState()
    {
        _enemy.SetResetTriggers("Chase");

        if(_enemy.Agent.enabled == true)
        {
            _enemy.Agent.destination = _enemy.Player.position;
        }
        

        //Debug.Log("Entered Chase");
    }

    public override void ExitState()
    {
        //Debug.Log("Exited Chase");
    }

    public override EnemyState GetNextState()
    {
        if (_enemy.currentHealth <= 0) return EnemyState.Dead;

        if (_enemy.DistanceToPlayer() <= _enemy.AttackRange) return EnemyState.Attack;
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

            if (_enemy.currentHealth < 15)
            {
                _enemy.Animator.SetFloat("Speed", isFacingMoveDirection ? Mathf.Clamp(animdir.z, 0f, _enemy.damagedSpeed) : Mathf.Floor(0), .75f, Time.deltaTime);
                _enemy.Agent.speed = Mathf.Clamp(animdir.z, 0f, _enemy.damagedSpeed);
            }
            else
            {
                _enemy.Animator.SetFloat("Speed", isFacingMoveDirection ? Mathf.Clamp(animdir.z, 0f, _enemy.maxSpeed) : Mathf.Floor(0), .75f, Time.deltaTime);
                _enemy.Agent.speed = Mathf.Clamp(animdir.z, 0f, _enemy.maxSpeed);
            }

            if (Vector3.Distance(_enemy.transform.position, _enemy.Agent.destination) < _enemy.Agent.radius)
            {
                _enemy.Agent.ResetPath();
            }
            else if (!_enemy.isInQueue)
            {
                _enemy.Agent.destination = _enemy.Player.position;
            }
        }
        else
        {
            _enemy.Animator.SetFloat("Speed", Mathf.Floor(0), .5f, Time.deltaTime);
        }
    }
}
