using UnityEngine;

public class BackDodgeStateFSM : BaseState<EnemyState>
{
    private BaseEnemyAI _enemy;
    public BackDodgeStateFSM(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    public override void EnterState()
    {
        Debug.Log("Entered BackDodge State");

        // Stop movement and face player
        _enemy.StopMoving();
        _enemy.RotateToPlayer();

        // Reset all triggers
        _enemy.ResetTriggers();

        _enemy.isDodging = true;

        // Trigger anim
        _enemy.Animator.SetTrigger("BackDodge");
    }

    public override void ExitState()
    {
        _enemy.ResetTriggers();
        _enemy.ResumeMoving();
    }

    public override EnemyState GetNextState()
    {
        float dist = _enemy.DistanceToPlayer();

        if (dist <= _enemy.ChaseRange && !_enemy.isDodging)
        {
            if (dist <= _enemy.AttackRange)
            {
                return EnemyState.Attack;
            }
            return EnemyState.Chase;
        }

        return StateKey;
    }

    public override void UpdateState()
    {
        // Should be move the enemt backwards
    }
}
