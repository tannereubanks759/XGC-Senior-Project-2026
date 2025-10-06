using UnityEngine;

public class AttackState : BaseState<EnemyState>
{
    // Reference to the enemy AI using this state.
    private BaseEnemyAI _enemy;

    private bool moveTowardsPlayer;

    private Vector3 attackDirection; // lock-in direction for this attack

    private float transitionDelay = 0.25f;
    private float transitionTime;

    public AttackState(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    public override void EnterState()
    {
        _enemy.attackTime = Time.time;

        //Debug.Log(_enemy.CurrentAttackState);
        // set the attack
        _enemy.Attack();

        Debug.Log(_enemy.currentAttack.name);

        //Debug.Log(_enemy.CurrentAttackState);

        // Trigger the attack state in the animator
        _enemy.SetResetTriggers("Attack");

        // if we can move during this attack (from anim data)
        moveTowardsPlayer = _enemy.canMoveWhileAttacking;

        if (moveTowardsPlayer)
        {
            // lock direction toward player at the start of attack
            attackDirection = (_enemy.Player.position - _enemy.transform.position).normalized;
            attackDirection.y = 0f; // keep horizontal
        }

        // stop navmesh/pathfinding so script controls motion
        _enemy.StopMoving();
    }

    public override void ExitState()
    {
        _enemy.SetAttackState(BaseEnemyAI.EAttackState.None);
        _enemy.ResumeMoving();
    }

    public override EnemyState GetNextState()
    {
        if (_enemy.CurrentAttackState == BaseEnemyAI.EAttackState.Finished && transitionTime == 0.0f)
        {
            transitionTime = Time.time;
        }
        if (transitionTime != 0.0f && Time.time - transitionTime >= transitionDelay)
        {
            //Debug.Log("Here");
            transitionTime = 0.0f;
            return EnemyState.Combat;
        }

        return StateKey;
    }

    public override void UpdateState()
    {
        // can we move next frame (set in anim event)
        moveTowardsPlayer = _enemy.canMoveWhileAttacking;

        if (_enemy.currentAttack != null && _enemy.CurrentAttackState == BaseEnemyAI.EAttackState.InProgress)
        {
            if (moveTowardsPlayer)
            {
                Vector3 moveDir = _enemy.moveBackward ? -attackDirection : attackDirection;
                _enemy.Agent.Move(moveDir * _enemy.combatSpeed * Time.deltaTime * _enemy.currentAttack.movModifier);
            }
        }
    }
}