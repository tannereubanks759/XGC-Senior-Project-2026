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
    public CombatController playerController;
    public GameObject lightingBoltPrefab;
    //public CombatQueue combatQueue { get; private set; }
    #endregion

    #region Vision System
    [Header("Vision System")]
    [Tooltip("Is this enemy using Line of Sight detection? (If not, the default is omnitient prox detection)")]
    public bool usingLoS;
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
    #endregion

    #region Lighting System
    [Header("Lighting Variables")]
    public float radius;
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

    [Tooltip("Is this unit currently blocking?")]
    public bool isBlocking;               // Flag for blocking state

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

    public bool isInQueue = false;

    public bool isRanged = false;
    #endregion

    #region Item System
    [Header("Item System")]
    [Tooltip("The item to be dropped (Leave empty if no item is to be spawned)")]
    public GameObject item;

    private GameObject _item;   // Private reference to the item being dropped/spawned

    [Tooltip("Whether or not the enemy will drop an item on death or not")]
    private bool hasItem;
    #endregion

    #region Gold System
    [Header("Gold System")]
    [Tooltip("The particle system of gold that spawns when then enemy dies")]
    [SerializeField] private ParticleSystem ps;
    private ParticleSystem _ps;
    [Tooltip("The amount of gold this enemy is to drop")]
    public int gold;
    #endregion

    #region Speed/Movement
    [Header("Speed/Movement System")]
    [Tooltip("The max speed of the unit")]
    public float maxSpeed = .5f;
    [Tooltip("The current speed of the unit")]
    public float CurrentSpeed { get; private set; } // Current movement speed
    public float damagedSpeed {  get; private set; }
    #endregion

    #region Health
    [Header("Health")]
    [Tooltip("The maximum amount of health this unit has")]
    [SerializeField] private int maxHealth = 100;           // Maximum health

    [Tooltip("The current amount of health the unit has")]
    public int currentHealth { get; private set; } // Current health
    #endregion
    #endregion

    #region Monobehavior Methods
    // Awake is called when the script instance is loaded
    protected virtual void Awake()
    {
        //StartCoroutine(Refs());
        //VarInit();
        ItemInit();
    }
    private void Start()
    {
        if (!Agent) Agent = GetComponent<NavMeshAgent>();
        if (!Animator) Animator = GetComponent<Animator>();
        if (!swordCollider) swordCollider = GetComponentInChildren<AffectPlayer>().swordCollider;
        VarInit();
    }
    // Call the update of the parent so that state logic still runs
    // Check to see if we can see the player
    void Update()
    {
        base.Update();

        if (usingLoS)
        {
            CanSeePlayer();
        }
        else
        {
            ProxCheck();
        }
    }
    #endregion

    #region Init Methods
    // Initialize references
    private void RefInit(GameObject p)
    {
        // Get refrences
        if(!Player) Player = p.transform;
        //if (!combatQueue) combatQueue = p.GetComponentInChildren<CombatQueue>();
        if (!playerController) playerController = p.GetComponentInChildren<CombatController>();
    }

    // Initialize variables
    private void VarInit()
    {
        Agent.stoppingDistance = AttackRange - 0.5f;
        currentHealth = maxHealth;
        isBlocking = false;

        canRunAtPlayer = false;
        combatRange = 8f;
        canMoveWhileAttacking = false;
        swordCollider.enabled = false;
        moveBackward = false;
        damagedSpeed = maxSpeed / 2f;

        gold = Random.Range(0, 51);
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

    // Initialize a particle system for the enemy
    // Plays once when the enemy dies
    public void GoldInit(int boneCount)
    {
        _ps = Instantiate(ps, transform.parent);
        _ps.Stop();
        var ap = _ps.GetComponent<AttractParticles>();
        ap.goldCount = 20 + 5 * boneCount;
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

        return canSeePlayerNow;
    }

    bool ProxCheck()
    {
        if (Player == null) return false;

        if (DistanceToPlayer() <= detectionRadius)
            canSeePlayerNow = true;

        return canSeePlayerNow;
    }

    #endregion

    #region Movement Methods

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

    // Called via animation event at the start of the swing
    public void OnAttackStart()
    {
        SetAttackState(EAttackState.InProgress);
        //Debug.Log(CurrentAttackState);
        //StopMoving();
        //RotateToPlayer();
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

        if (!isRanged) swordCollider.enabled = false;

        // APPLY DAMAGE
    }

    // Called via animation event at the end of the swing
    public void OnAttackEnd()
    {
        // Debug.Log("Enemy Attack End!");
        SetAttackState(EAttackState.Finished);
        //Debug.Log(CurrentAttackState);
        //canRotate = true;
        if (!isRanged) swordCollider.enabled = false;

        //overrideAttack = false;

        SetResetTriggers("AttackOver");
    }

    // Called via animation event at the end of the block
    public void OnBlockEnd()
    {
        SetResetTriggers("BlockOver");
        isBlocking = false;
    }

    public void BlockHitOver()
    {
        Debug.Log("HitOver");
        isBlocking = false;
        SetResetTriggers("BlockHitOver");
    }

    // Manually set the current attack state
    public void SetAttackState(EAttackState newState)
    {
        CurrentAttackState = newState;
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
        if(currentHealth > 0)
        {
            int finalDamage = damage;

            if (isBlocking)
            {
                // Halve incoming damage when blocking
                finalDamage = 0;
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
                }
                else
                {
                    //TransitionToState(EnemyState.Hit);

                    Animator.SetLayerWeight(1, 1f);
                    SetResetTriggers("HitLayer");
                }
            }
        }
        
    }

    // Handle death of the enemy
    public virtual void Die()
    {
        var tempTrans = this.transform;
        DropItem();
        DropGold(tempTrans);
        Debug.Log($"{name} died.");
        TransitionToState(EnemyState.Dead);
        
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

    // Drop gold
    public void DropGold(Transform transform)
    {
        _ps.Clear();
        _ps.transform.position = transform.position;
        _ps.Play();
    }

    private void SpawnLightningArc(Transform start, Transform end)
    {
        var lightning = Instantiate(lightingBoltPrefab);
        MonoBehaviour bolt = null;
        foreach (var scriptType in lightning.GetComponents<MonoBehaviour>())
        {
            if (scriptType.GetType().Name == "LightningBoltPrefabScript")
            {
                bolt = scriptType;
                break;
            }
        }
        var t = bolt.GetType();
        var fSource = t.GetField("Source"); if (fSource != null) fSource.SetValue(bolt, start.gameObject);
        var fDest = t.GetField("Destination"); if (fDest != null) fDest.SetValue(bolt, end.gameObject);
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
            var sd = other.transform.root.GetComponent<swordDamageDeterminer>();
            int damage = sd.damage;
            if (sd.isLighting)
            {
                
                TakeDamage(damage);
                //float radius = 10f;
                float damageMultiplier = 0.5f;
                Transform lastDamaged = this.transform;
                Collider[] closeEnemies = Physics.OverlapSphere(transform.position, radius, ~0, QueryTriggerInteraction.Ignore);
                foreach (Collider col in closeEnemies)
                {
                    if (col.CompareTag("Enemy") && col.transform != this.transform)
                    {
                        var enemyTestScript = col.GetComponent<BasicSkeleton>();
                        if (enemyTestScript.currentHealth > 0)
                        {
                            //Debug.Log("Lighting damage transferred");
                            Vector3 offset = Vector3.up * 1.3f;
                            Transform enemyEnder = new GameObject("enemyEnderPoint").transform;
                            Transform enemyLast = new GameObject("lastDamagedPoint").transform;
                            enemyEnder.position = enemyTestScript.transform.position + offset;
                            enemyLast.position = lastDamaged.transform.position + offset;
                            //Vector3 offset = enemyTestScript.transform.position + Vector3.up;
                            enemyTestScript.TakeDamage(Mathf.RoundToInt(damage * damageMultiplier));
                            //SpawnLightningArc(lastDamaged, enemyTestScript.transform);
                            SpawnLightningArc(enemyLast, enemyEnder);
                            lastDamaged = enemyTestScript.transform;
                        }
                        
                    }

                }
                var lantern = FindFirstObjectByType<chargeOffHandLatern>();
                lantern.hitRegistered();
            }
            else
            {
                
                TakeDamage(damage);
                var lantern = FindFirstObjectByType<chargeOffHandLatern>();
                lantern.hitRegistered();
            }
        }
    }

    public void EndHitAnim()
    {
        Animator.SetLayerWeight(1, 0f);
    }
    #endregion

    #region Triggers & Misc
    // Reset all the triggers, then set the correct one
    public void SetResetTriggers(string trigger)
    {
        foreach (var p in Animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger)
            {
                Animator.ResetTrigger(p.name);
            }
        }
        Animator.SetTrigger(trigger);
    }

    public float SnapZero(float value, float threshold = 0.01f)
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

        /*
        if (Agent.hasPath)
        {
            for (var i = 0; i < Agent.path.corners.Length - 1; i++)
            {
                Debug.DrawLine(Agent.path.corners[i] , Agent.path.corners[i + 1], Color.blue);
            }
        }
        */
    }
    #endregion
}