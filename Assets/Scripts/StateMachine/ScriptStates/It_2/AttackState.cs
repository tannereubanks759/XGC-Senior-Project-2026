using UnityEngine;

public class AttackState : BaseState<EnemyState>
{
    // Reference to the enemy AI using this state.
    private BaseEnemyAI _enemy;

    private bool moveTowardsPlayer;

    public AttackState(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    public override void EnterState()
    {
        _enemy.Attack();
        moveTowardsPlayer = _enemy.CanMoveWhileAttacking();
    }

    public override void ExitState()
    {
        
    }

    public override EnemyState GetNextState()
    {
        
        return StateKey;
    }

    public override void UpdateState()
    {
        
    }
}