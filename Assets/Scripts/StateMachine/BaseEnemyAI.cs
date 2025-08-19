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

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth { get; private set; }

    [Header("Speeds")]
    public float WalkSpeed = 2f;
    public float RunSpeed = 5f;
    public float CurrentSpeed { get; private set; } // Current active movement speed

    [Header("Patrolling")]
    public Transform[] PatrolPoints;

    [Header("Attack Control")]
    public float AttackCooldown = 1.5f;
    private float lastAttackTime;

    protected virtual void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Animator = GetComponent<Animator>();
        Player = GameObject.FindGameObjectWithTag("Player")?.transform;
        currentHealth = maxHealth;
    }

    // Shared movement
    public void MoveTo(Vector3 destination)
    {
        if (Agent == null)
            return;
        
        Agent.SetDestination(destination);
    }


    public void SetSpeed(float speed)
    {
        CurrentSpeed = speed;
        Agent.speed = speed;
    }

    public void StopMoving()
    {
        if (Agent != null)
        {
            //Agent.ResetPath();
            Agent.isStopped = true;
        }
    }

    public void ResumeMoving()
    {
        Agent.isStopped = false;
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

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"{name} took {damage} damage. Health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            TransitionToState(EnemyState.Hit);
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        // Check if collided with the player's sword
        if (other.CompareTag("PlayerSword"))
        {
            // get damage from sword component if needed
            int damage = 10;
            TakeDamage(damage);
        }
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