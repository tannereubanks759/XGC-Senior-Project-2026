using UnityEngine;

public class BasicBlockState : BaseState<EnemyState>
{
    private BaseEnemyAI _enemy;
    public BasicBlockState(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    public override void EnterState()
    {
        //Debug.Log("Entered Block State");
        _enemy.SetResetTriggers("Block");

        _enemy.isBlocking = true;

    }

    public override void ExitState()
    {
        //Debug.Log("Exited Block State");        
    }

    public override EnemyState GetNextState()
    {
        if (_enemy.currentHealth <= 0) return EnemyState.Dead;

        if (!_enemy.isBlocking)
        {
            return EnemyState.Chase;
        }
        return StateKey;
    }

    public override void UpdateState()
    {

    }
}
