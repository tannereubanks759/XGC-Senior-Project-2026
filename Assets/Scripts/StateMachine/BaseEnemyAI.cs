/*
 * BaseEnemyAI.cs
 * 
 * This script defines the base AI for an enemy using a finite state machine.
 * It handles movement, attacks, damage, death, patrols, and interactions
 * with the player. States like Idle, Patrol, Chase, Attack, Hit, Block,
 * BackDodge, and Dead are all managed through the inherited StateManager.
 * 
 * By: Matthew Bolger
*/

using UnityEngine;
using UnityEngine.AI;

public class BaseEnemyAI : StateManager<EnemyState>
{
    [Header("References")]
    public Transform Player;              // Reference to the player's transform
    public NavMeshAgent Agent;            // NavMeshAgent for pathfinding/movement
    public Animator Animator;             // Animator controlling enemy animations

    [Header("Vision & Ranges")]
    public float ChaseRange = 10f;        // Distance at which enemy will start chasing
    public float AttackRange = 2f;        // Distance at which enemy will attack

    [Header("Health")]
    public int maxHealth = 100;           // Maximum health
    public int currentHealth { get; private set; } // Current health

    [Header("Speeds")]
    public float WalkSpeed = 2f;          // Patrol speed
    public float RunSpeed = 5f;           // Chase/attack speed
    public float CurrentSpeed { get; private set; } // Current movement speed
    public bool canRotate;                // Whether the enemy can rotate toward player

    [Header("Damage/Combat")]
    public int Damage;                     // Base damage (used in attacks)
    public bool isBlocking;               // Flag for blocking state
    public bool isDodging;                // Flag for dodging state
    private infoscript playerInfo;        // Reference to player's info (e.g., key count)

    // Attack state enum to track attack animation progress
    public enum AttackState { None, InProgress, Finished }

    [Header("Attack Control")]
    public AttackState CurrentAttackState { get; private set; } = AttackState.None;

    // Awake is called when the script instance is loaded
    protected virtual void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Animator = GetComponent<Animator>();
        playerInfo = GameObject.FindGameObjectWithTag("PlayerInfo").GetComponent<infoscript>();
        currentHealth = maxHealth;
        canRotate = true;
        isBlocking = false;
        isDodging = false;
    }

    #region Movement Methods
    // Move the enemy toward a destination using NavMeshAgent
    public void MoveTo(Vector3 destination)
    {
        if (Agent == null) return;

        if (!Agent.isStopped)
            Agent.SetDestination(destination);
    }

    // Set the dodging flag to false (used after a dodge ends)
    public void SetIsDodgingFalse()
    {
        isDodging = false;
    }

    // Set movement speed for the NavMeshAgent
    public void SetSpeed(float speed)
    {
        CurrentSpeed = speed;
        Agent.speed = speed;
    }

    // Stop the enemy's movement immediately
    public void StopMoving()
    {
        if (Agent != null)
        {
            Agent.isStopped = true;
            Agent.velocity = Vector3.zero;
        }
    }

    // Resume movement if it was previously stopped
    public void ResumeMoving()
    {
        Agent.isStopped = false;
    }

    // Allow the enemy to rotate toward the player
    public void RotateToPlayer()
    {
        if (Agent != null)
        {
            Agent.updateRotation = true;
        }
    }

    // Calculate distance to the player
    public float DistanceToPlayer()
    {
        if (Player == null)
            return Mathf.Infinity;

        return Vector3.Distance(transform.position, Player.position);
    }
    #endregion

    #region Patrol Area
    // Find the closest PatrolArea in the scene
    public PatrolArea FindClosestPatrolArea()
    {
        PatrolArea[] areas = FindObjectsByType<PatrolArea>(FindObjectsSortMode.None);
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
        Attack();        // Apply damage logic here
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

    // Manually set the current attack state
    public void SetAttackState(AttackState newState)
    {
        CurrentAttackState = newState;
    }

    // Virtual attack logic (override in subclasses)
    public virtual void Attack()
    {
        Debug.Log($"{name} attacks!");
    }

    // Quick checks for attack states
    public bool IsAttackInProgress => CurrentAttackState == AttackState.InProgress;
    public bool IsAttackFinished => CurrentAttackState == AttackState.Finished;

    // Reset attack state to none
    public void ResetAttackState()
    {
        CurrentAttackState = AttackState.None;
    }
    #endregion

    #region Damage and Death Methods
    // Apply damage to the enemy, factoring in blocking
    public void TakeDamage(int damage)
    {
        if (isBlocking)
        {
            TakeDamageBlocking(damage);
        }
        else
        {
            currentHealth -= damage;
        }

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

    // Apply reduced damage and knockback when blocking
    void TakeDamageBlocking(int damage)
    {
        currentHealth -= Mathf.FloorToInt(damage / 2);

        Rigidbody playerRB = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<Rigidbody>();

        if (playerRB != null)
        {
            Vector3 knockbackVec = -Player.forward;
            playerRB.AddForce(knockbackVec, ForceMode.Impulse);
        }
    }

    // Handle death of the enemy
    public virtual void Die()
    {
        Debug.Log($"{name} died.");
        TransitionToState(EnemyState.Dead);

        if (playerInfo.keyCount > 0)
        {
            // Optional: give player a key on death
        }
    }

    // Called when colliding with triggers
    public void OnTriggerEnter(Collider other)
    {
        // Detect sword hits
        if (other.CompareTag("PlayerSword"))
        {
            int damage = 10; // Can be retrieved from sword component if needed
            TakeDamage(damage);
        }
    }
    #endregion

    #region Triggers & Misc
    // Reset all animator triggers
    public void ResetTriggers()
    {
        Animator.ResetTrigger("Idle");
        Animator.ResetTrigger("Chase");
        Animator.ResetTrigger("Dead");
        Animator.ResetTrigger("Attack");
        Animator.ResetTrigger("Patrol");
    }

    // Draw gizmos in editor to visualize ranges
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, ChaseRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRange);
    }
    #endregion
}