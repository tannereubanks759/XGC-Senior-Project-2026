using UnityEngine;

public class DeadState : BaseState<EnemyState>
{
    private BaseEnemyAI _enemy;

    public DeadState(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    public override void EnterState()
    {
        Debug.Log($"{_enemy.name} entered DEAD state.");

        _enemy.StopMoving();

        // Optionally disable NavMeshAgent or AI components
        if (_enemy.Agent != null)
        {
            _enemy.Agent.enabled = false;
        }

        // Optional: disable collider or play death animation
        Collider col = _enemy.GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        Animator animator = _enemy.GetComponent<Animator>();
        if (animator != null)
            animator.SetTrigger("Die");
    }

    public override void ExitState()
    {
        // Nothing to do — dead enemies don't come back (in this case).
    }

    public override void UpdateState()
    {
        // Dead enemies don't update behavior.
    }

    public override EnemyState GetNextState()
    {
        // Always remain in Dead state.
        return StateKey;
    }
}
