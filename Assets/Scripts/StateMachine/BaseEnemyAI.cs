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
    public bool canRotate;

    [Header("Patrolling")]
    public Vector3[] PatrolPoints;

    public enum AttackState { None, InProgress, Finished }

    [Header("Attack Control")]
    public AttackState CurrentAttackState { get; private set; } = AttackState.None;

    protected virtual void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Animator = GetComponent<Animator>();
        Player = GameObject.FindGameObjectWithTag("Player")?.transform;
        currentHealth = maxHealth;
        canRotate = true;
    }

    #region Movement Methods
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
            Agent.isStopped = true;
            Agent.velocity = Vector3.zero;
        }
    }
    public void ResumeMoving()
    {
        Agent.isStopped = false;
    }

    public void RotateToPlayer()
    {
        if (Agent != null)
        {
            Agent.updateRotation = true;
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
    #endregion

    #region Patrol Area

    public PatrolArea FindClosestPatrolArea()
    {
        PatrolArea[] areas = FindObjectsOfType<PatrolArea>();
        PatrolArea closest = null;
        float minDistance = Mathf.Infinity;

        foreach (var area in areas)
        {
            float dist = Vector3.Distance(transform.position, area.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = area;
            }
        }

        return closest;
    }
    #endregion

    #region Attack Methods 
    // Called via animation event at the start of the swing
    public void StartAttack()
    {
        Debug.Log("Enemy Attack Start!");
        CurrentAttackState = AttackState.InProgress;
        StopMoving();
        RotateToPlayer();
    }

    // Called via animation event at the apex of the swing
    public void OnAttackHit()
    {
        Debug.Log("Enemy Attack Hit!");
        Attack(); // Apply damage here
        canRotate = false;
    }

    // Called via animation event at the end of the swing
    public void OnAttackEnd()
    {
        Debug.Log("Enemy Attack End!");
        CurrentAttackState = AttackState.Finished;
        canRotate = true;
        ResumeMoving();
    }

    // Sets the attack state
    public void SetAttackState(AttackState newState)
    {
        CurrentAttackState = newState;
    }

    // Attack logic
    public virtual void Attack()
    {
        Debug.Log($"{name} attacks!");
    }

    // Checkers
    public bool IsAttackInProgress => CurrentAttackState == AttackState.InProgress;
    public bool IsAttackFinished => CurrentAttackState == AttackState.Finished;

    // Reset attack state
    public void ResetAttackState()
    {
        CurrentAttackState = AttackState.None;
    }
    #endregion

    #region Damage and Death Methods
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
    public virtual void Die()
    {
        Debug.Log($"{name} died.");
        TransitionToState(EnemyState.Dead);
        // Play animation, disable collider, etc.
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

    #endregion

    #region Triggers & Misc
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
    #endregion
}