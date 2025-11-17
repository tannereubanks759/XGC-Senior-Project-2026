using UnityEngine;

public class BasicIdleState : BaseState<EnemyState>
{
    private BaseEnemyAI _enemy;
    private float idleTime;
    private float idleInterval = 5f;

    private float lastAttackTime;
    private const float attackCooldown = 1f;
    public BasicIdleState(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    public override void EnterState()
    {
        _enemy.Animator.SetFloat("Speed", _enemy.SnapZero(Mathf.Floor(0f)));

        idleTime = Time.time;

        idleInterval = Random.value * 5;

        //Debug.Log("Entered Idle");
    }

    public override void ExitState()
    {
        //Debug.Log("Exited Idle");
    }

    public override EnemyState GetNextState()
    {
        if (_enemy.currentHealth <= 0) return EnemyState.Dead;

        if (Time.time - idleTime >= idleInterval && _enemy.canMove)
        {
            return EnemyState.Patrol;
        }

        if (_enemy.canSeePlayerNow)
        {
            // Only allow stationary enemies to attack once per cooldown
            if (!_enemy.canMove)
            {
                if (Time.time - lastAttackTime >= attackCooldown)
                {
                    lastAttackTime = Time.time;
                    return EnemyState.Attack;
                }
            }
            else
            {
                // Mobile enemies can attack immediately
                return EnemyState.Attack;
            }
        }

        return StateKey;
    }

    public override void UpdateState()
    {
    }
}
