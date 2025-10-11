using Unity.VisualScripting;
using UnityEngine;

public class BasicIdleState : BaseState<EnemyState>
{
    private BaseEnemyAI _enemy;
    private float idleTime;
    private float idleInterval = 5f;
    public BasicIdleState(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    public override void EnterState()
    {
        _enemy.Animator.SetFloat("Speed", Mathf.Floor(0f));

        idleTime = Time.time;

        Debug.Log("Entered Idle");
    }

    public override void ExitState()
    {
        Debug.Log("Exited Idle");
    }

    public override EnemyState GetNextState()
    {
        if (Time.time - idleTime >= idleInterval)
        {
            return EnemyState.Patrol;
        }
        return StateKey;
    }

    public override void UpdateState()
    {
    }
}
