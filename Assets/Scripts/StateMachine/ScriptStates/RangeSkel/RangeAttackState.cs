using UnityEngine;

public class RangeAttackState : BaseState<EnemyState>
{
    private BaseEnemyAI _enemy;
    private float attackTime;
    private float attackStartTime;
    private bool isTrackingPlayer = true;

    
    private const float trackDuration = 1f;

    
    private const float stopBlendSpeed = 0.125f;

    
    private const float rotationSpeed = 180f;

    public RangeAttackState(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    public override void EnterState()
    {
        _enemy.SetAttackState(BaseEnemyAI.EAttackState.InProgress);

        _enemy.SetResetTriggers("Attack");

        attackStartTime = Time.time;
        attackTime = 0;
        isTrackingPlayer = true;

        // Only stop the agent if the enemy can move
        if (_enemy.canMove)
            _enemy.Agent.isStopped = true;

        // Ensure idle speed for animation blending
        _enemy.Animator.SetFloat("Speed", 0f, stopBlendSpeed, Time.deltaTime);

        Debug.Log("Entered Attack");
    }

    public override void ExitState()
    {
        _enemy.SetAttackState(BaseEnemyAI.EAttackState.None);
        _enemy.Agent.isStopped = false; // resume navigation
        Debug.Log("Exited Attack");
    }

    public override EnemyState GetNextState()
    {
        if (_enemy.currentHealth <= 0)
            return EnemyState.Dead;

        // Attack finished
        if (_enemy.CurrentAttackState == BaseEnemyAI.EAttackState.None)
        {
            if (_enemy.canMove)
                return EnemyState.Chase; // mobile enemies continue chasing
            else
                return EnemyState.Idle;  // stationary enemies go back to idle
        }

        return StateKey;
    }

    public override void UpdateState()
    {
        float elapsed = Time.time - attackStartTime;

        // Rotate toward player only for the first part of the attack
        if (isTrackingPlayer && elapsed < trackDuration)
        {
            Vector3 dir = (_enemy.Player.position - _enemy.transform.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(dir);
            _enemy.transform.rotation = Quaternion.RotateTowards(_enemy.transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
        else
        {
            isTrackingPlayer = false;
        }

        // Ensure we stay visually idle (root motion keeps us grounded)
        _enemy.Animator.SetFloat("Speed", 0f, 0.125f, Time.deltaTime);

        // Handle attack timing reset
        if (_enemy.CurrentAttackState == BaseEnemyAI.EAttackState.Finished && attackTime == 0)
            attackTime = Time.time;

        if (Time.time - attackTime >= 0.5f && _enemy.CurrentAttackState == BaseEnemyAI.EAttackState.Finished)
        {
            _enemy.SetAttackState(BaseEnemyAI.EAttackState.None);
            attackTime = 0;
        }
    }
}
