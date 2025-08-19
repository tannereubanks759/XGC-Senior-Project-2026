using UnityEngine;

public class HitState : BaseState<EnemyState>
{
    private BaseEnemyAI _enemy;
    private float hitDuration = 0.7f; // how long the flinch lasts
    private float enterTime;

    public HitState(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    public override void EnterState()
    {
        Debug.Log("Entered Hit State");
        _enemy.StopMoving();
        _enemy.Animator.SetTrigger("Hit");
        enterTime = Time.time;
    }

    public override void ExitState()
    {
        Debug.Log("Exiting Hit State");
        _enemy.ResetTriggers();
        _enemy.ResumeMoving();
    }

    public override void UpdateState()
    {
        // Wait for the hit animation duration
    }

    public override EnemyState GetNextState()
    {
        if (_enemy.currentHealth <= 0)
            return EnemyState.Dead;

        // After hit animation is done
        if (Time.time - enterTime >= hitDuration)
        {
            float dist = _enemy.DistanceToPlayer();

            if (dist <= _enemy.AttackRange)
            {
                return EnemyState.Attack;
            }

            if (dist <= _enemy.ChaseRange)
            {
                return EnemyState.Chase;
            }

            return EnemyState.Idle;
        }

        return StateKey;
    }
}
