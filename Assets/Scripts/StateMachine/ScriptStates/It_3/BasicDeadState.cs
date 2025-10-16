using UnityEngine;
using Unity.VisualScripting;

public class BasicDeadState : BaseState<EnemyState>
{
    private BaseEnemyAI _enemy;

    public BasicDeadState(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    public override void EnterState()
    {
        //Debug.Log("Entered Dead State");

        var deathIndex = Random.Range(0, 2);

        _enemy.Animator.SetInteger("DeadIndex", deathIndex);

        _enemy.SetResetTriggers("Dead");

        _enemy.Animator.SetFloat("Speed", Mathf.Floor(0));

        _enemy.Agent.ResetPath();

        _enemy.Agent.enabled = false;

        _enemy.GetComponent<Collider>().enabled = false;

        //_enemy.combatQueue.RemoveAttackingEnemy(_enemy.GetComponent<BasicSkeleton>());

        // Needs work
        _enemy.AddComponent<DeathCull>();
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
