using UnityEngine;

public class BasicHitState : BaseState<EnemyState>
{
    private BaseEnemyAI _enemy;
    private float hitTime;
    public BasicHitState(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    public override void EnterState()
    {
        //Debug.Log("Entered Hit State");
        _enemy.SetResetTriggers("Hit");

        _enemy.Animator.SetFloat("Speed", Mathf.Floor(0));

        hitTime = Time.time;
    }

    public override void ExitState()
    {
        //Debug.Log("Exited Hit State");
    }

    public override EnemyState GetNextState()
    {
        if (_enemy.currentHealth <= 0) return EnemyState.Dead;

        if (Time.time - hitTime >= 0.6f)
        {
            if (_enemy.DistanceToPlayer() < _enemy.AttackRange) return EnemyState.Attack;

            return EnemyState.Chase;
        }
        return StateKey;
    }

    public override void UpdateState()
    {
    }
}
