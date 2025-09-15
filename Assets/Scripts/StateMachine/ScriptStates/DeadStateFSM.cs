using UnityEngine;

public class DeadStateFSM : BaseState<EnemyState>
{
    private BaseEnemyAI _enemy;
    private GameObject _key;

    public DeadStateFSM(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    public override void EnterState()
    {
        Debug.Log($"{_enemy.name} entered DEAD state.");

        _enemy.Animator.SetTrigger("Dead");

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
