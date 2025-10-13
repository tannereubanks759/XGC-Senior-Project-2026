using UnityEngine;

public class BasicChaseState : BaseState<EnemyState>
{
    private BaseEnemyAI _enemy;
    private BasicSkeleton _skeleton;

    public BasicChaseState(EnemyState key, BaseEnemyAI enemy, BasicSkeleton skeleton) : base(key)
    {
        _enemy = enemy;
        _skeleton = skeleton;
    }

    public override void EnterState()
    {
        _enemy.SetResetTriggers("Chase");

        _enemy.Agent.destination = _enemy.Player.position;

        //Debug.Log("Entered Chase");
    }

    public override void ExitState()
    {
        //Debug.Log("Exited Chase");
    }

    public override EnemyState GetNextState()
    {
        if (_enemy.currentHealth <= 0) return EnemyState.Dead;

        // Runs on multiple frames so the % chance to block needs to be small so that it doesn't always block
        var roll = Random.value;
        if (_enemy.currentHealth < 100 
            && _enemy.DistanceToPlayer() <= _enemy.threatRange 
            && _enemy.PlayerIsAttacking()
            && _skeleton.isShieldedEnemy
            && roll <= .10) return EnemyState.Block;

        //                                              ----&& !_enemy.isInQueue
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

            _enemy.Animator.SetFloat("Speed", isFacingMoveDirection ? animdir.z : Mathf.Floor(0), .75f, Time.deltaTime);

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
