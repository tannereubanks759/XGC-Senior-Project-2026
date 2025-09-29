
using UnityEngine;

public class HitState : BaseState<EnemyState>
{
    // Reference to the enemy AI using this state.
    private BaseEnemyAI _enemy;

    private float hitTime;
    private float hitThreshold = 1f;

    public HitState(EnemyState key, BaseEnemyAI enemy) : base(key)
    {
        _enemy = enemy;
    }

    public override void EnterState()
    {
        _enemy.StopMoving();

        _enemy.SetResetTriggers("Hit");

        _enemy.Animator.SetFloat("hitVer", Random.value);

        hitTime = Time.time;
    }

    public override void ExitState()
    {
        _enemy.ResumeMoving();
    }

    public override EnemyState GetNextState()
    {
        // Always check for death first
        if (_enemy.currentHealth <= 0)
            return EnemyState.Dead;

        // Wait until hit animation duration is over
        if (Time.time - hitTime >= hitThreshold)
        {
            float distance = _enemy.DistanceToPlayer();

            // Player is in attack range transition to combat
            if (distance <= _enemy.combatRange)
                return EnemyState.Combat;

            // Player is outside combat range chase
            if (distance > _enemy.combatRange)
                return EnemyState.Run;
        }

        return StateKey;
    }

    public override void UpdateState()
    {
        // Face the player while hit
        if (_enemy.Player != null)
        {
            Vector3 lookDir = (_enemy.Player.position - _enemy.transform.position).normalized;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
                _enemy.transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }
}
