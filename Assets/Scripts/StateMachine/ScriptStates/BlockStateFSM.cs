using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class BlockStateFSM : BaseState<EnemyState>
{
    private BaseEnemyAI _enemy;

    // The time the enemy started blocking at
    private float blockingTime;

    // The animator controlling this entity
    private Animator _animator;

    // The anim info (NEEDS TO BE AFTER THE .SETRIGGER())
    private AnimatorStateInfo stateInfo;

    public BlockStateFSM(EnemyState key, BaseEnemyAI enemy, Animator animator) : base(key)
    {
        _enemy = enemy;
        _animator = animator;
    }

    public override void EnterState()
    {
        Debug.Log("Entered Block State");

        // Stop movement and face player
        _enemy.StopMoving();
        _enemy.RotateToPlayer();

        // Reset all triggers
        _enemy.ResetTriggers();

        // Trigger anim
        _enemy.Animator.SetTrigger("Block");

        // Get the state info
        stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

        // Set the blocking time
        blockingTime = Time.time;

        _enemy.isBlocking = true;
    }

    public override void ExitState()
    {
        // Reset animator triggers and resume movement
        _enemy.ResetTriggers();
        _enemy.ResumeMoving();
        _enemy.isBlocking = false;
    }

    public override void UpdateState()
    {
        if (_enemy.isBlocking)
        {
            //_enemy.transform.position = -_enemy.transform.forward * Time.deltaTime;
        }    
    }

    public override EnemyState GetNextState()
    {
        float dist = _enemy.DistanceToPlayer();

        if (Time.time - blockingTime >= stateInfo.length)
        {
            if (dist <= _enemy.ChaseRange)
            {
                if (dist <= _enemy.AttackRange)
                {
                    return EnemyState.Attack;
                }
                return EnemyState.Chase;
            }

            return EnemyState.Idle;
        }
        return StateKey;
    }
}
