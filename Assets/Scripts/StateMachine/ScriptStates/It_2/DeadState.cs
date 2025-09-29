using UnityEngine;

public class DeadState : BaseState<EnemyState>
{
    // Reference to the enemy AI using this state.
    private BaseEnemyAI _enemy;

    public DeadState(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    public override void EnterState()
    {
        _enemy.StopMoving();

        float deadVer = Random.value > 0.5f ? 1f : 0f;

        _enemy.Animator.SetFloat("dVer", deadVer);

        _enemy.SetResetTriggers("Dead");
    }

    public override void ExitState()
    {
        // Nothing
    }

    public override EnemyState GetNextState()
    {
        return StateKey;
    }

    public override void UpdateState()
    {
        // Nothing
    }
}
