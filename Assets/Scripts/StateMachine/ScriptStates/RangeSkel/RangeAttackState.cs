using UnityEngine;

public class RangeAttackState : BaseState<EnemyState>
{
    private BaseEnemyAI _enemy;
    private float attackTime;
    private float attackStartTime;
    private bool isTrackingPlayer = true;

    // How long enemy keeps facing the player during attack (seconds)
    private const float trackDuration = 0.7f;

    // How fast the enemy blends to idle at the start of attack
    private const float stopBlendSpeed = 0.125f;

    // How fast to rotate when tracking
    private const float rotationSpeed = 360f;

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

        // Stop the NavMeshAgent from steering movement, 
        // but keep it active for rotation/position reference
        _enemy.Agent.isStopped = true;

        // Begin blending the movement animation toward idle
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

        if (_enemy.CurrentAttackState == BaseEnemyAI.EAttackState.None)
            return EnemyState.Chase;

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
        _enemy.Animator.SetFloat("Speed", 0f, 0.2f, Time.deltaTime);

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
