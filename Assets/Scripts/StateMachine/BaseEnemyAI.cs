using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.AI;

public class BaseEnemyAI : StateManager<EnemyState>
{
    [Header("References")]
    public Transform Player;
    public NavMeshAgent Agent;
    public Animator Animator;

    [Header("Vision & Ranges")]
    public float ChaseRange = 10f;
    public float AttackRange = 2f;

    [Header("Patrolling")]
    public Transform[] PatrolPoints;

    protected virtual void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Animator = GetComponent<Animator>();
    }

    // Shared movement
    public void MoveTo(Vector3 destination)
    {
        if (Agent != null)
            Agent.SetDestination(destination);
    }

    public void StopMoving()
    {
        if (Agent != null)
        {
            Agent.ResetPath();
        }
    }

    public float DistanceToPlayer()
    {
        if (Player == null)
        {
            return Mathf.Infinity;
        }

        return Vector3.Distance(transform.position, Player.position);
    }

    public virtual void Attack()
    {
        Debug.Log($"{name} attacks!");
    }

    public virtual void Die()
    {
        Debug.Log($"{name} died.");
        TransitionToState(EnemyState.Dead);
        // Play animation, disable collider, etc.
    }

    public void ResetTriggers()
    {
        Animator.ResetTrigger("Idle");
        Animator.ResetTrigger("Chase");
        Animator.ResetTrigger("Dead");
        Animator.ResetTrigger("Attack");
        Animator.ResetTrigger("Patrol");
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, ChaseRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRange);
    }
}
