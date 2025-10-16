using UnityEngine;

public class BasicAttackState : BaseState<EnemyState>
{
    private BaseEnemyAI _enemy;
    private float attackTime;
    public BasicAttackState(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    public override void EnterState()
    {
        //Debug.Log("Entered Attack");

        _enemy.SetAttackState(BaseEnemyAI.EAttackState.InProgress);

        _enemy.SetResetTriggers("Attack");

        var attackIndex = Random.Range(0, 4);

        _enemy.Animator.SetInteger("AttackIndex", attackIndex);

    }

    public override void ExitState()
    {
        //Debug.Log("Exited Attack");

        _enemy.SetAttackState(BaseEnemyAI.EAttackState.None);
    }

    public override EnemyState GetNextState()
    {
        if (_enemy.currentHealth <= 0) return EnemyState.Dead;

        if (_enemy.CurrentAttackState == BaseEnemyAI.EAttackState.None)
        {
            return EnemyState.Chase;
        }
        return StateKey;
    }

    public override void UpdateState()
    {
        if (_enemy.Agent.hasPath)
        {
            var dir = (_enemy.Player.position - _enemy.transform.position).normalized;
            var animdir = _enemy.transform.InverseTransformDirection(dir);
            var isFacingPlayer = Vector3.Dot(dir, _enemy.transform.forward) > .25f;

            _enemy.transform.rotation = Quaternion.RotateTowards(_enemy.transform.rotation, Quaternion.LookRotation(dir), 180 * Time.deltaTime);

            _enemy.Animator.SetFloat("Speed", isFacingPlayer ? Mathf.Clamp(animdir.z, 0f, 0.125f) : Mathf.Floor(0), .75f, Time.deltaTime);
            _enemy.Agent.speed = Mathf.Clamp(animdir.z, 0f, 0.125f);
        }

        if (_enemy.CurrentAttackState == BaseEnemyAI.EAttackState.Finished && attackTime == 0)
        {
            attackTime = Time.time;
        }

        if (Time.time - attackTime >= .5f && _enemy.CurrentAttackState == BaseEnemyAI.EAttackState.Finished)
        {
            _enemy.SetAttackState(BaseEnemyAI.EAttackState.None);
            attackTime = 0;
        }
    }
}
