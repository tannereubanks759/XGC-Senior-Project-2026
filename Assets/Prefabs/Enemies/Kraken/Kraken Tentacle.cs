using UnityEngine;

public class KrakenTentacle : MonoBehaviour
{
    [Header("Runtime")]
    public bool isDropping = false;

    [Tooltip("Player transform. If left null, the script will try to find an object tagged 'Player'.")]
    public Transform player;

    [Tooltip("Set by KrakenDangerArea.")]
    public bool playerInDangerArea = false;

    private Vector3 originXZ; // keep original XZ for returning when player leaves range

    [Header("Water Surface (Hover Y Source)")]
    [Tooltip("Optional: assign a water surface Transform (recommended). Hover Y will follow this Transform's Y.")]
    public Transform waterSurface;

    [Tooltip("If no waterSurface Transform is provided, this value is used as the water surface Y.")]
    public float waterSurfaceY = 0f;

    [Tooltip("Hover height above the water surface (world units). This becomes the target's Y when not attacking.")]
    public float hoverAboveWater = 6f;

    [Header("Follow Settings")]
    [Tooltip("Optional world-space offset while hovering (X/Z useful; Y is ignored because hover Y comes from water).")]
    public Vector3 hoverOffset = Vector3.zero;

    [Tooltip("How fast the target moves while hovering over the player.")]
    public float followSpeed = 12f;

    [Tooltip("How fast the target moves back upward after an attack.")]
    public float riseSpeed = 12f;

    [Tooltip("If true, X/Z will match the player (plus hoverOffset).")]
    public bool matchPlayerXZ = true;

    [Header("Attack Settings")]
    [Tooltip("How far above the player's position to aim the drop (roughly head height).")]
    public float dropToHeight = 1.6f;

    [Tooltip("How fast the target drops downward.")]
    public float dropSpeed = 25f;

    [Tooltip("Random time (seconds) before dropping once hovering and eligible.")]
    public Vector2 timeBetweenDropsRange = new Vector2(0.75f, 2.5f);

    [Tooltip("Cooldown after an attack ends before another drop can start.")]
    public float attackCooldown = 3.0f;

    [Tooltip("Safety timeout: if we don't collide in this many seconds while dropping, we force GoUp().")]
    public float maxDropTime = 2.0f;

    [Tooltip("Require the target to be at least this much above the player before it can decide to drop.")]
    public float requiredAbovePlayerToDrop = 1.0f;

    private bool isReturning = false;

    private float dropCountdown = 0f;
    private float nextAttackAllowedTime = 0f;
    private float dropElapsed = 0f;

    void Start()
    {
        originXZ = transform.position;
        originXZ.y = GetHoverY(); // keep origin's Y locked to water-based hover

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        ResetDropCountdown();
    }

    void Update()
    {
        // recover player if needed
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        // If no player, just return to origin
        if (player == null)
        {
            isDropping = false;
            isReturning = false;

            Vector3 home = originXZ;
            home.y = GetHoverY();
            MoveTargetTowards(home, riseSpeed);
            return;
        }

        // If player leaves while dropping, cancel and go up
        if (!playerInDangerArea && isDropping)
        {
            isDropping = false;
            GoUp();
        }

        // Player not in range: return to origin and reset
        if (!playerInDangerArea)
        {
            isDropping = false;
            isReturning = false;

            Vector3 home = originXZ;
            home.y = GetHoverY();
            MoveTargetTowards(home, riseSpeed);

            ResetDropCountdown();
            return;
        }

        // In range behavior
        if (isDropping)
        {
            dropElapsed += Time.deltaTime;

            Vector3 dropPos = player.position + Vector3.up * dropToHeight;
            if (!matchPlayerXZ)
            {
                // straight vertical stab from current XZ
                dropPos.x = transform.position.x;
                dropPos.z = transform.position.z;
            }

            MoveTargetTowards(dropPos, dropSpeed);

            // Safety timeout: if no collision happens, recover
            if (dropElapsed >= maxDropTime)
            {
                isDropping = false;
                GoUp();
            }

            return;
        }

        if (isReturning)
        {
            Vector3 hoverPos = GetHoverPosition();
            MoveTargetTowards(hoverPos, riseSpeed);

            if ((transform.position - hoverPos).sqrMagnitude <= 0.05f * 0.05f)
            {
                isReturning = false;
                ResetDropCountdown();
            }
            return;
        }

        // Hover follow: fixed Y from water surface, track player XZ
        Vector3 desiredHover = GetHoverPosition();
        MoveTargetTowards(desiredHover, followSpeed);

        bool cooldownReady = Time.time >= nextAttackAllowedTime;
        bool aboveEnough = (transform.position.y - player.position.y) >= requiredAbovePlayerToDrop;

        if (cooldownReady && aboveEnough)
        {
            dropCountdown -= Time.deltaTime;
            if (dropCountdown <= 0f)
                StartDrop();
        }
        else
        {
            // prevent instant drop the moment it becomes eligible
            ResetDropCountdown();
        }
    }

    // called when the tentacle touches a player, or collides with the ground.
    // Moves the target back up to hover (water-based Y) and starts cooldown.
    public void GoUp()
    {
        nextAttackAllowedTime = Time.time + attackCooldown;

        isReturning = true;
        dropElapsed = 0f;

        ResetDropCountdown();
    }

    private void StartDrop()
    {
        isDropping = true;
        dropElapsed = 0f;

        // don’t reroll while actively dropping
        dropCountdown = Mathf.Infinity;
    }

    private float GetHoverY()
    {
        float waterY = waterSurface ? waterSurface.position.y : waterSurfaceY;
        return waterY + hoverAboveWater;
    }

    private Vector3 GetHoverPosition()
    {
        Vector3 p = player.position + hoverOffset;

        if (!matchPlayerXZ)
        {
            p.x = transform.position.x;
            p.z = transform.position.z;
        }

        p.y = GetHoverY(); // <- ALWAYS derived from water surface
        return p;
    }

    private void MoveTargetTowards(Vector3 targetPos, float speed)
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
    }

    private void ResetDropCountdown()
    {
        float min = Mathf.Max(0f, timeBetweenDropsRange.x);
        float max = Mathf.Max(min, timeBetweenDropsRange.y);
        dropCountdown = Random.Range(min, max);
    }
}
