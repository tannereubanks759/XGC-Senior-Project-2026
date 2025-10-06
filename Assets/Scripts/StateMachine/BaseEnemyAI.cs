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

//using UnityEditorInternal;
using UnityEngine;
using UnityEngine.AI;

public class BaseEnemyAI : StateManager<EnemyState>
{
    #region VARIABLES
    #region References
    // NEW VARIABLES
    [Header("References")]
    [Tooltip("A reference to the player's position")]
    public Transform Player;                                    // Reference to the player's transform
    public NavMeshAgent Agent { get; private set; }             // NavMeshAgent for pathfinding/movement
    public Animator Animator { get; private set; }              // Animator controlling enemy animations
    private Collider swordCollider;                             // Reference to the collider attached to the weapon
    private CombatController playerController;
    #endregion

    #region Vision System
    [Header("Vision System")]
    [Tooltip("How far this unit can see the player (line of sight check included)")]
    public float detectionRadius = 12f;

    [Tooltip("Field of view angle in degrees")]
    [Range(0, 360)]
    public float fieldOfView = 120f;

    [Tooltip("Which layers count as obstructions (e.g., walls, terrain)")]
    public LayerMask obstructionMask;

    [Tooltip("Which layers are considered players")]
    public LayerMask playerMask;

    [HideInInspector] public bool canSeePlayerNow { get; private set; }
    [HideInInspector] public Vector3 lastKnownPlayerPos { get; private set; }

    //[Tooltip("Event for the player being spotted")]
    //public event Action OnPlayerSpotted;
    //[Tooltip("Event for the player being lost")]
    //public event Action OnPlayerLost;
    #endregion

    #region Combat System
    [Header("Combat System")]
    [Tooltip("Can the enemy run towards the player")]
    public bool canRunAtPlayer;

    [HideInInspector]
    public bool hasSeenPlayerBefore = false;

    [Tooltip("The range in which the enemy will start to engage in combat")]
    public float combatRange;

    [Tooltip("The range in which this unit will start to attack the player (Auto braking is hard coded to stop the enemies 0.5 units into the attack range)")]
    public float AttackRange = 2.5f;        // Distance at which enemy will attack

    [Tooltip("The range in which the enemy will react to the player's attacks")]
    public float threatRange = 4f;

    // Attack state enum to track attack animation progress
    public enum EAttackState { None, InProgress, Finished }

    [Tooltip("Enum that tells us what state the enemy attack is in." +
        "\n(Set in anim events)")]
    public EAttackState CurrentAttackState = EAttackState.None;

    [Tooltip("The amount of damage that this unit will do to the player")]
    public int Damage { get; private set; }                    // Base damage (used in attacks)

    [Tooltip("Can this unit move toward the player while attacking?" +
        "\n(Decided based on the attack animation")]
    public bool canMoveWhileAttacking;
    
    [Tooltip("Will this unit move backward?" +
        "\n(Decided based based on the attack animation)")]
    public bool moveBackward;

    [Tooltip("An array containing the attack data")]
    public AttackData[] attacks;

    [HideInInspector] public AttackData currentAttack;

    [HideInInspector] public bool overrideAttack = false;

    [HideInInspector] public float attackTime;              // The time that the enemy attacked

    [Tooltip("The time in which the enemy must wait before attacking again")]
    public float attackCooldown = 3f;
    #endregion

    #region Item System
    [Header("Item System")]
    [Tooltip("The item to be dropped (Leave empty if no item is to be spawned)")]
    public GameObject item;

    private GameObject _item;   // Private reference to the item being dropped/spawned

    [Tooltip("Whether or not the enemy will drop an item on death or not")]
    private bool hasItem;
    #endregion

    #region Speed/Movement
    [Header("Speeds")]
    [Tooltip("The speed this unit will move at while walking")]
    public float WalkSpeed = 2f;          // Patrol speed

    [Tooltip("The speed this unit will move at while running")]
    public float RunSpeed = 5f;           // Chase/attack speed

    [Tooltip("The current speed of the unit")]
    public float CurrentSpeed { get; private set; } // Current movement speed

    [Tooltip("Can this unit rotate? (Set to off for certain combat actions)")]
    public bool canRotate;                // Whether the enemy can rotate toward player

    public float combatSpeed = 4f; // slightly faster than walk, slower than run
    #endregion

    #region Health
    [Header("Health")]
    [Tooltip("The maximum amount of health this unit has")]
    [SerializeField] private int maxHealth = 100;           // Maximum health

    [Tooltip("The current amount of health the unit has")]
    public int currentHealth { get; private set; } // Current health
    #endregion

    // OLD VARIABLES

    [Header("Vision & Ranges")]
    [Tooltip("The range in which this unit will spot the player")]
    public float ChaseRange = 10f;        // Distance at which enemy will start chasing

    [Header("Damage/Combat")]
    [Tooltip("Is this unit currently blocking?")]
    public bool isBlocking;               // Flag for blocking state
    [Tooltip("Is this unit currently dodging?")]
    public bool isDodging;                // Flag for dodging state
    
    #endregion

    #region Monobehavior Methods
    // Awake is called when the script instance is loaded
    protected virtual void Awake()
    {
        RefInit();
        VarInit();
        ItemInit();
    }

    // Call the update of the parent so that state logic still runs
    // Check to see if we can see the player
    void Update()
    {
        base.Update();

        CanSeePlayer();
    }
    #endregion

    #region Init Methods
    // Initialize references
    private void RefInit()
    {
        // Get refrences
        Player = GameObject.FindGameObjectWithTag("Player").transform;
        Agent = GetComponent<NavMeshAgent>();
        Animator = GetComponent<Animator>();
        swordCollider = GetComponentInChildren<AffectPlayer>().swordCollider;
        playerController = Player.GetComponentInChildren<CombatController>();
    }

    // Initialize variables
    private void VarInit()
    {
        Agent.stoppingDistance = AttackRange - 0.5f;
        currentHealth = maxHealth;
        canRotate = true;
        isBlocking = false;
        isDodging = false;

        canRunAtPlayer = false;
        combatRange = 8f;
        canMoveWhileAttacking = false;
        swordCollider.enabled = false;
        moveBackward = false;
    }

    // Initialize an item system for the enemy
    // As long as the item to be dropped is set,
    // the item logic will run
    private void ItemInit()
    {
        // Set the bool if the enemy has an item
        hasItem = item != null ? true : false;

        // Spawn a key if the enemy has one and turn it off.
        if (hasItem)
        {
            _item = Instantiate(item, transform.parent);
            _item.SetActive(false);
        }
    }
    #endregion

    #region Vision Methods
    // Vision check
    public bool CanSeePlayer()
    {
        bool wasSeeingPlayer = canSeePlayerNow;
        canSeePlayerNow = false;

        // Check if player exists
        if (Player == null) return false;

        // Step 1: Within detection radius?
        float distance = Vector3.Distance(transform.position, Player.position);
        if (distance > detectionRadius) return false;

        // Step 2: Within FOV?
        Vector3 dirToPlayer = (Player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToPlayer);

        if (angle > fieldOfView / 2f) return false;

        // Step 3: Line of sight (raycast)
        if (!Physics.Raycast(transform.position + Vector3.up * 1.5f, dirToPlayer, distance, obstructionMask))
        {
            canSeePlayerNow = true;
            lastKnownPlayerPos = Player.position;
        }

        // Fire events only if status changed
        //if (!wasSeeingPlayer && canSeePlayerNow)
        //    OnPlayerSpotted?.Invoke();
        //else if (wasSeeingPlayer && !canSeePlayerNow)
        //    OnPlayerLost?.Invoke();

        //Debug.Log("Distance to player: " + distance + ", angle: " + angle + ", can see player: " + (canSeePlayerNow ? "YES" : "NO"));

        return canSeePlayerNow;
    }

    #endregion

    #region Movement Methods
    // Move the enemy toward a destination using NavMeshAgent
    public void MoveTo(Vector3 destination)
    {
        if (Agent == null) return;

        Agent.isStopped = Agent.isStopped ? false : true;

        if (!Agent.isStopped)
            Agent.SetDestination(destination);
    }

    public void RunTowardsPlayer()
    {
        if (Player == null) return;

        Vector3 direction = (Player.position - transform.position).normalized;

        // Move manually
        transform.position += direction * RunSpeed * Time.deltaTime;

        // Rotate toward player
        if (canRotate) transform.rotation = Quaternion.LookRotation(direction);

        // Play the run animation trigger if not already playing
        if (!Animator.GetCurrentAnimatorStateInfo(0).IsName("Run")) SetResetTriggers("Run");
    }

    public void CanRunAtPlayer()
    {
        canRunAtPlayer = !canRunAtPlayer;
    }

    // Called by states to drive movement animations
    public void SetMovementInput(Vector3 worldDirection)
    {
        // If you’re using NavMeshAgent:
        // Convert world direction into local space relative to enemy forward
        Vector3 localDir = transform.InverseTransformDirection(worldDirection.normalized);

        // Push into animator
        Animator.SetFloat("xMov", localDir.x, 0.1f, Time.deltaTime); // smoothing with damp
        Animator.SetFloat("zMov", localDir.z, 0.1f, Time.deltaTime);
    }

    //
    public void DirectMove(Vector3 moveVector)
    {
        if (Agent != null && Agent.enabled)
        {
            // Temporarily disable path recalculation
            Agent.ResetPath();

            // Move directly while respecting NavMesh
            Agent.Move(moveVector);
        }
    }

    // Set the dodging flag to false (used after a dodge ends)
    public void SetIsDodgingFalse()
    {
        isDodging = false;
        SetResetTriggers("Combat");
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

    // Tell the navmesh to not update the agents postion
    public void AgentUpdateOff()
    {
        if (Agent == null)
        {
            Agent.isStopped = true;
            Agent.updatePosition = false;
            Agent.updateRotation = false;
        }
    }

    // Turn the Agent back on
    public void AgentUpdateOn()
    {
        if (Agent == null)
        {
            Agent.updateRotation = true;
            Agent.updatePosition = true;
            Agent.isStopped = false;
        }
    }

    // Helper to disable the agent
    public void DisableAgent()
    {
        Agent.isStopped = true;
        Agent.updatePosition = false;
        Agent.updateRotation = false;
    }

    // Helper to enable the agent
    public void EnableAgent()
    {
        Agent.isStopped = false;
        Agent.updatePosition = true;
        Agent.updateRotation = true;
    }

    // Warp the agent to the enemies postion
    public void WarpAgent()
    {
        Agent.Warp(transform.position);
    }

    // Resume movement if it was previously stopped
    public void ResumeMoving()
    {
        Agent.isStopped = false;
    }

    // Allow the enemy to rotate toward the player
    public void RotateToPlayer()
    {
        if (Player == null) return;

        Vector3 dir = (Player.position - transform.position).normalized;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 10f * Time.deltaTime);
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

    #region Patrol Area Methods
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
    //
    public bool PlayerIsAttacking()
    {
        if (Player == null) return false;

        return playerController.swinging;
    }

    public void StopMoveWhileAttacking()
    {
        canMoveWhileAttacking = false;
        moveBackward = false;
    }

    public void StartMoveWhileAttacking()
    {
        canMoveWhileAttacking = true;
    }

    public void MoveBackwardWhileAttacking()
    {
        moveBackward = true;
        canMoveWhileAttacking = true;
    }    

    // Slowly stop the enemies movement to (0, 0, 0)
    public void SlowStopMovement(float x, float z)
    {
        // Set vars
        bool setAValue = false;

        // The enemy is moving in the x direction
        if (SnapZero(x) != 0)
        {
            x -= 0.06f * Time.deltaTime;

            Animator.SetFloat("xMov", x);

            setAValue = true;
        }
        if (SnapZero(z) != 0)
        {
            z -= 0.06f * Time.deltaTime;

            Animator.SetFloat("zMov", z);

            setAValue = true;
        }
        if (setAValue)
        {
            SlowStopMovement(x, z);
        }
    }

    // Called via animation event at the start of the swing
    public void OnAttackStart()
    {
        SetAttackState(EAttackState.InProgress);
        //Debug.Log(CurrentAttackState);
        StopMoving();
        RotateToPlayer();
        //swordCollider.enabled = true;
    }

    public void EnableSwordCollider()
    {
        swordCollider.enabled = true;
    }

    // Called via animation event at the apex of the swing
    public void OnAttackHit()
    {
        //Debug.Log("Enemy Attack Hit!");
        //Attack();        // Apply damage logic here
        //canRotate = false;

        swordCollider.enabled = false;

        // APPLY DAMAGE
    }

    // Called via animation event at the end of the swing
    public void OnAttackEnd()
    {
        // Debug.Log("Enemy Attack End!");
        SetAttackState(EAttackState.Finished);
        //Debug.Log(CurrentAttackState);
        canRotate = true;
        swordCollider.enabled = false;
        overrideAttack = false;

        SetResetTriggers("AttackOver");
    }

    // Called via animation event at the end of the block
    public void OnBlockEnd()
    {
        isBlocking = false;
        SetResetTriggers("Combat");
    }

    // Manually set the current attack state
    public void SetAttackState(EAttackState newState)
    {
        CurrentAttackState = newState;
    }

    // Virtual attack logic (override in subclasses)
    public virtual void Attack()
    {
        Debug.Log("Can Override: " + overrideAttack);
        // Pick one of 5 slots in your blend tree
        int attackIndex = Random.Range(0, attacks.Length);
        currentAttack = attacks[attackIndex];

        Debug.Log("Rand Index: " + attackIndex);

        // Set the parameter for the blend tree
        Animator.SetInteger("AttackIndex", attackIndex);

        canMoveWhileAttacking = currentAttack.canMoveDuringAttack;
        moveBackward = currentAttack.movesBackward;

        //if (overrideAttack) OverrideAttack();
    }

    // Override the attack selected in the attack state
    public void OverrideAttack()
    {
        currentAttack = attacks[attacks.Length - 1];

        Debug.Log("Override Index: " + (attacks.Length - 1));

        // Set the parameter for the blend tree
        Animator.SetInteger("AttackIndex", attacks.Length - 1);

        canMoveWhileAttacking = currentAttack.canMoveDuringAttack;
        moveBackward = currentAttack.movesBackward;
    }

    // Quick checks for attack states
    public bool IsAttackInProgress => CurrentAttackState == EAttackState.InProgress;
    public bool IsAttackFinished => CurrentAttackState == EAttackState.Finished;

    public float ThreatRange { get; internal set; }

    // Reset attack state to none
    public void ResetAttackState()
    {
        CurrentAttackState = EAttackState.None;
    }
    #endregion

    #region Damage and Death Methods
    // Apply damage to the enemy, factoring in blocking
    

    public void TakeDamage(int damage)
    {
        int finalDamage = damage;
        
        

        if (isBlocking)
        {
            // Halve incoming damage when blocking
            finalDamage = Mathf.FloorToInt(damage / 2);
            Debug.Log($"{name} blocked! Damage reduced to {finalDamage}.");
        }

        // Apply damage
        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"{name} took {finalDamage} damage. Health: {currentHealth}");

        // Death check
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Pick correct reaction state
            if (isBlocking)
            {
                //TransitionToState(EnemyState.BlockHit);
                SetResetTriggers("BlockHit");
            }
            else
            {
                TransitionToState(EnemyState.Hit);
            }
        }
    }

    public void BlockHitOver()
    {
        Debug.Log("HitOver");
        SetResetTriggers("BlockHitOver");
    }



    // Handle death of the enemy
    public virtual void Die()
    {
        Debug.Log($"{name} died.");
        TransitionToState(EnemyState.Dead);
        DropItem();
    }

    // Drop an item if applicable
    public void DropItem()
    {
        if (hasItem)
        {
            _item.transform.position = this.transform.position;
            _item.transform.rotation = this.transform.rotation;
            _item.SetActive(true);
        }
    }

    // Called when colliding with triggers
    public void OnTriggerEnter(Collider other)
    {
        // Detect sword hits
        if (other.CompareTag("PlayerSword"))
        {
            if (isBlocking)
            {
                Debug.Log("Stagger");
                Player.gameObject.GetComponentInChildren<CombatController>().GetStaggeredFrom(this.transform, 1f); //Stagger player if enemy gets hit while blocking
            }
            swordDamageDeterminer sd = other.transform.root.GetComponent<swordDamageDeterminer>();

            int damage = sd.damage; // Can be retrieved from sword component if needed
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
        Animator.ResetTrigger("Block");
        Animator.ResetTrigger("BlockHit");
        Animator.ResetTrigger("Hit");
        Animator.ResetTrigger("BackDodge");
    }

    // Reset all the triggers, then set the correct one
    public void SetResetTriggers(string trigger)
    {
        Animator.ResetTrigger("Idle");
        Animator.ResetTrigger("Patrol");
        Animator.ResetTrigger("Warcry");
        Animator.ResetTrigger("Run");
        Animator.ResetTrigger("Emote1");
        Animator.ResetTrigger("Emote2");
        Animator.ResetTrigger("Emote3");
        Animator.ResetTrigger("Combat");
        Animator.ResetTrigger("Hit");
        Animator.ResetTrigger("Dead");
        Animator.ResetTrigger("Attack");
        Animator.ResetTrigger("AttackOver");
        Animator.ResetTrigger("Chase");
        Animator.ResetTrigger("Block");
        Animator.ResetTrigger("BlockHit");
        Animator.ResetTrigger("BlockHitOver");
        Animator.ResetTrigger("BackDodge");
        Animator.ResetTrigger("EmoteOver");
        Animator.SetTrigger(trigger);
    }

    float SnapZero(float value, float threshold = 0.01f)
    {
        return Mathf.Abs(value) < threshold ? 0f : value;
    }

    // Set the movement floats in the animator
    public void SetAnimatorMovement(float x, float z)
    {
        Animator.SetFloat("xMov", SnapZero(x));
        Animator.SetFloat("zMov", SnapZero(z));
    }


    // Draw gizmos in editor to visualize ranges
    protected void OnDrawGizmosSelected()
    {
        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRange);

        // Draw vision cone
        Vector3 fovLine1 = Quaternion.AngleAxis(fieldOfView / 2, Vector3.up) * transform.forward * detectionRadius;
        Vector3 fovLine2 = Quaternion.AngleAxis(-fieldOfView / 2, Vector3.up) * transform.forward * detectionRadius;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, fovLine1);
        Gizmos.DrawRay(transform.position, fovLine2);

        // If currently seeing the player, draw green line
        if (canSeePlayerNow && Player != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, Player.position);
        }
    }
    #endregion
}