// CHANGE LOG
// 
// CHANGES || version VERSION
//
// "Enable/Disable Headbob, Changed look rotations - should result in reduced camera jitters" || version 1.0.1

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting;


#if UNITY_EDITOR
using UnityEditor;
    using System.Net;
#endif

public class FirstPersonController : MonoBehaviour
{
    // --- Swimming (ported) ---
    [Header("Swimming")]
    public bool enableSwimming = true;
    public float swimSpeed = 6f;
    public float swimUpForce = 6f;          // vertical swim thrust
    public float waterDrag = 1f;            // drag while in water (Unity 6: linearDamping)
    public float normalDrag = 0.5f;         // drag on land
    public LayerMask waterLayer;            // (optional) water layer if you prefer layers over tags
    public KeyCode swimUpKey = KeyCode.Space;
    public KeyCode swimDownKey = KeyCode.LeftControl;
    // --- Water surface + sinking until camera submerged ---
    [Header("Water Surface & Buoyancy")]
    public Transform waterSurface;            // optional: set to the water plane transform (its Y = surface)
    public float sinkAcceleration = 4f;       // how strongly you sink before submerging
    public float submergeOffset = 0.00f;      // small tolerance; 0 = camera must go strictly below surface

    // Internal: captured from trigger when you enter water (if waterSurface is not assigned)
    private float currentWaterSurfaceY = float.NaN;

    private bool isSwimming = false;
    private float storedDrag = 0f;
    private bool storedUseGravity = true;

    public CombatController healthSystem;


    private Rigidbody rb;

    #region Camera Movement Variables

    public Camera playerCamera;

    public float fov = 60f;
    public bool invertCamera = false;
    public bool cameraCanMove = true;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 50f;

    // Crosshair
    public bool lockCursor = true;
    public bool crosshair = true;
    public Sprite crosshairImage;
    public Color crosshairColor = Color.white;

    // Internal Variables
    private float yaw = 0.0f;
    private float pitch = 0.0f;
    private Image crosshairObject;

    #region Camera Zoom Variables

    public bool enableZoom = true;
    public bool holdToZoom = false;
    public KeyCode zoomKey = KeyCode.Mouse1;
    public float zoomFOV = 30f;
    public float zoomStepTime = 5f;

    // Internal Variables
    private bool isZoomed = false;

    #endregion
    #endregion

    #region Movement Variables

    public bool playerCanMove = true;
    public float walkSpeed = 5f;
    public float maxVelocityChange = 10f;

    //added
    // Air control tuning
    public bool preserveAirMomentum = true;
    public float airAcceleration = 2.5f;  // small accel toward input while airborne
    public float airMaxVelocityChange = 1.5f; // cap per FixedUpdate in air
                                              // Cache input each frame so Update drives UI and FixedUpdate drives physics
    private Vector3 cachedInput;
    private bool cachedHasInput;



    // Internal Variables
    private bool isWalking = false;

    #region Sprint

    public bool enableSprint = true;
    public bool unlimitedSprint = false;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public float sprintSpeed = 7f;
    public float sprintDuration = 5f;
    public float sprintCooldown = .5f;
    public float sprintFOV = 80f;
    public float sprintFOVStepTime = 10f;

    // Sprint Bar
    public bool useSprintBar = true;
    public bool hideBarWhenFull = true;
    public Image sprintBarBG;
    public Image sprintBar;
    public float sprintBarWidthPercent = .3f;
    public float sprintBarHeightPercent = .015f;

    // Internal Variables
    private CanvasGroup sprintBarCG;
    private bool isSprinting = false;
    private float sprintRemaining;
    private float sprintBarWidth;
    private float sprintBarHeight;
    private bool isSprintCooldown = false;
    private float sprintCooldownReset;

    #endregion

    #region Jump

    public bool enableJump = true;
    public KeyCode jumpKey = KeyCode.Space;
    public float jumpPower = 5f;

    // Internal Variables
    public bool isGrounded = false;

    #endregion

    #region Crouch

    public bool enableCrouch = true;
    public bool holdToCrouch = true;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public float crouchHeight = .75f;
    public float speedReduction = .5f;

    // Internal Variables
    private bool isCrouched = false;
    private Vector3 originalScale;

    #endregion
    #endregion

    #region Head Bob

    public bool enableHeadBob = true;
    public Transform joint;
    public float bobSpeed = 10f;
    public Vector3 bobAmount = new Vector3(.15f, .05f, 0f);

    // Internal Variables
    private Vector3 jointOriginalPos;
    private float timer = 0;


    public static bool isPaused = false;

    // --- Slope Limit ---
    [Header("Slope Limit")]
    public bool enableSlopeLimit = true;
    [Range(0f, 89f)] public float slopeLimit = 45f;

    [Tooltip("If true, you will slide down when standing on slopes steeper than the limit.")]
    public bool slideOnSteep = true;

    [Tooltip("Acceleration down the steep slope (m/s^2).")]
    public float slideGravity = 10f;

    [Tooltip("Multiplier on slide speed to tame runaway velocity (0 = none).")]
    [Range(0f, 1f)] public float slideFriction = 0.15f;

    // Internal (slope state)
    private Vector3 groundNormal = Vector3.up;
    private float groundAngle = 0f;
    private Vector3 groundPoint;
    private bool OnSteepSlope => enableSlopeLimit && (groundAngle > slopeLimit + 0.1f);

    // --- Fall Damage ---
    [Header("Fall Damage")]
    public bool enableFallDamage = true;

    // No damage until you fall farther than this (meters)
    [Tooltip("No damage until you fall farther than this (meters).")]
    public float minFallHeight = 3.0f;

    // At/above this height the hit is lethal (set high if you don't want one-shot)
    [Tooltip("At/above this height the impact is lethal.")]
    public float lethalFallHeight = 18f;

    // How much damage per meter beyond the safe threshold
    [Tooltip("Linear damage beyond minFallHeight (damage per extra meter).")]
    public int damagePerExtraMeter = 10;

    // Reduce damage when landing crouched (0.2 = 20% less damage)
    [Range(0f, 1f)]
    public float crouchDamageReduction = 0.2f;

    // --- Internals for fall tracking ---
    private bool wasGrounded = false;
    private bool isFalling = false;
    private float fallStartY = 0f;


    #endregion

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        crosshairObject = GetComponentInChildren<Image>();

        // Set internal variables
        playerCamera.fieldOfView = fov;
        originalScale = transform.localScale;
        jointOriginalPos = joint.localPosition;

        if (!unlimitedSprint)
        {
            sprintRemaining = sprintDuration;
            sprintCooldownReset = sprintCooldown;
        }
    }

    void Start()
    {
        if(lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        if(crosshair)
        {
            crosshairObject.sprite = crosshairImage;
            crosshairObject.color = crosshairColor;
        }
        else
        {
            crosshairObject.gameObject.SetActive(false);
        }
        waterSurface = GameObject.FindGameObjectWithTag("Water").gameObject.transform;
        #region Sprint Bar

        sprintBarCG = GetComponentInChildren<CanvasGroup>();

        if(useSprintBar)
        {
            sprintBarBG.gameObject.SetActive(true);
            sprintBar.gameObject.SetActive(true);

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            sprintBarWidth = screenWidth * sprintBarWidthPercent;
            sprintBarHeight = screenHeight * sprintBarHeightPercent;

            sprintBarBG.rectTransform.sizeDelta = new Vector3(sprintBarWidth, sprintBarHeight, 0f);
            sprintBar.rectTransform.sizeDelta = new Vector3(sprintBarWidth - 2, sprintBarHeight - 2, 0f);

            if(hideBarWhenFull)
            {
                sprintBarCG.alpha = 0;
            }
        }
        else
        {
            sprintBarBG.gameObject.SetActive(false);
            sprintBar.gameObject.SetActive(false);
        }

        #endregion

        healthSystem = GetComponentInChildren<CombatController>();
    }

    float camRotation;



    private void Update()
    {
        #region Camera
        if (isPaused) return;
        // Control camera movement
        if(cameraCanMove)
        {
            yaw = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * mouseSensitivity;

            if (!invertCamera)
            {
                pitch -= mouseSensitivity * Input.GetAxis("Mouse Y");
            }
            else
            {
                // Inverted Y
                pitch += mouseSensitivity * Input.GetAxis("Mouse Y");
            }

            // Clamp pitch between lookAngle
            pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

            transform.localEulerAngles = new Vector3(0, yaw, 0);
            playerCamera.transform.localEulerAngles = new Vector3(pitch, 0, 0);
        }

        #region Camera Zoom

        if (enableZoom)
        {
            // Changes isZoomed when key is pressed
            // Behavior for toogle zoom
            if(Input.GetKeyDown(zoomKey) && !holdToZoom && !isSprinting)
            {
                if (!isZoomed)
                {
                    isZoomed = true;
                }
                else
                {
                    isZoomed = false;
                }
            }

            // Changes isZoomed when key is pressed
            // Behavior for hold to zoom
            if(holdToZoom && !isSprinting)
            {
                if(Input.GetKeyDown(zoomKey))
                {
                    isZoomed = true;
                }
                else if(Input.GetKeyUp(zoomKey))
                {
                    isZoomed = false;
                }
            }

            // Lerps camera.fieldOfView to allow for a smooth transistion
            if(isZoomed)
            {
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, zoomFOV, zoomStepTime * Time.deltaTime);
            }
            else if(!isZoomed && !isSprinting)
            {
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, fov, zoomStepTime * Time.deltaTime);
            }
        }

        #endregion
        #endregion

        #region Sprint
        // Cache input for both UI logic and physics step
        cachedInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxis("Vertical"));
        cachedHasInput = cachedInput.sqrMagnitude > 0.0001f;

        #region Sprint
        if (enableSprint)
        {
            // Decide sprint state here so FOV/UI respond immediately
            bool wantsSprint = enableSprint && Input.GetKey(sprintKey) && sprintRemaining > 0f && !isSprintCooldown && cachedHasInput;
            isSprinting = wantsSprint;

            if (isSprinting)
            {
                // FOV while sprinting
                isZoomed = false;
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, sprintFOV, sprintFOVStepTime * Time.deltaTime);

                // Drain sprint time
                if (!unlimitedSprint)
                {
                    sprintRemaining -= 1f * Time.deltaTime;
                    if (sprintRemaining <= 0f)
                    {
                        isSprinting = false;
                        isSprintCooldown = true;
                    }
                }

                // Fade bar in while actually moving, if requested
                if (useSprintBar && hideBarWhenFull)
                    sprintBarCG.alpha = Mathf.Min(1f, sprintBarCG.alpha + 5f * Time.deltaTime);
            }
            else
            {
                // Not sprinting: lerp back to base FOV (unless zoom is active)
                if (!isZoomed)
                    playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, fov, sprintFOVStepTime * Time.deltaTime);

                // Regain sprint over time
                if (!unlimitedSprint)
                    sprintRemaining = Mathf.Clamp(sprintRemaining + 1f * Time.deltaTime, 0f, sprintDuration);

                // Handle cooldown
                if (isSprintCooldown)
                {
                    sprintCooldown -= 1f * Time.deltaTime;
                    if (sprintCooldown <= 0f) isSprintCooldown = false;
                }
                else
                {
                    sprintCooldown = sprintCooldownReset;
                }

                // Fade bar out when fully recharged
                if (useSprintBar && hideBarWhenFull && sprintRemaining >= sprintDuration - 0.0001f)
                    sprintBarCG.alpha = Mathf.Max(0f, sprintBarCG.alpha - 3f * Time.deltaTime);
            }

            // Keep your existing sprint bar fill scaling
            if (useSprintBar && !unlimitedSprint)
            {
                float sprintRemainingPercent = sprintRemaining / sprintDuration;
                sprintBar.transform.localScale = new Vector3(sprintRemainingPercent, 1f, 1f);
            }
        }
        #endregion


        #endregion

        #region Jump

        // Gets input and calls jump method
        // Disable jump while swimming (Space is used to ascend)
        if (!isSwimming && enableJump && Input.GetKeyDown(jumpKey) && isGrounded)
        {
            Jump();
        }


        #endregion

        #region Crouch

        if (enableCrouch)
        {
            if(Input.GetKeyDown(crouchKey) && !holdToCrouch)
            {
                Crouch();
            }
            
            if(Input.GetKeyDown(crouchKey) && holdToCrouch)
            {
                isCrouched = false;
                Crouch();
            }
            else if(Input.GetKeyUp(crouchKey) && holdToCrouch)
            {
                isCrouched = true;
                Crouch();
            }
        }

        #endregion

        CheckGround();
        HandleFallDamage();

        if (enableHeadBob && !isSwimming)
        {
            HeadBob();
        }
        isWalking = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).sqrMagnitude > 0.01f;

    }

    void FixedUpdate()
    {
        if (isPaused) return;
        if (!playerCanMove) return;

        // --- SWIMMING (camera-relative; Space up, Q down) ---
        if (isSwimming && enableSwimming)
        {
            float camY = playerCamera.transform.position.y;
            // Determine the current water surface height
            float waterY = !float.IsNaN(currentWaterSurfaceY)
                ? currentWaterSurfaceY
                : (waterSurface != null ? waterSurface.position.y : transform.position.y + 99999f); // fallback if not set

            // If the CAMERA is above the water surface, gently push the player down until submerged.
            // Allow Space to override (player intentionally swimming up).
            if (playerCamera != null)
            {
                
                if (camY > waterY - submergeOffset && !Input.GetKey(swimUpKey))
                {
                    // Apply downward acceleration (acts like gravity in water until your head goes under)
                    rb.AddForce(Vector3.down * sinkAcceleration, ForceMode.Acceleration);
                }
            }

            // --- your existing swim forces (camera-relative WASD + Space/Q) continue here ---
            Transform camT = playerCamera != null ? playerCamera.transform : transform;
            float h = cachedInput.x;
            float v = cachedInput.z;

            Vector3 fwd = camT.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = camT.right; right.y = 0f; right.Normalize();

            Vector3 horiz = (right * h + fwd * v);
            if (horiz.sqrMagnitude > 1f) horiz.Normalize();

            // --- Vertical input (with surface clamp) ---
            float upInput = 0f;
            //float camY = playerCamera.transform.position.y;
            float surfaceY = currentWaterSurfaceY;
            if (waterSurface != null) surfaceY = waterSurface.position.y;

            // Allow rising only if camera is still below surface
            if (Input.GetKey(swimUpKey) && camY < surfaceY - submergeOffset)
            {
                upInput += 1f;
            }

            // Descend always works
            if (Input.GetKey(swimDownKey))
            {
                upInput -= 1f;
            }

            Vector3 vert = camT.up * upInput;


            Vector3 swimAccel = horiz * swimSpeed + vert * swimUpForce;
            if (enableSprint && Input.GetKey(sprintKey)) swimAccel *= 1.15f;

            rb.AddForce(swimAccel, ForceMode.Acceleration);
            return;
        }


        // --- LAND/AIR MOVEMENT (exact same logic you used before) ---
        Vector3 wishDir = transform.TransformDirection(cachedInput).normalized;
        bool hasInput = cachedHasInput;

        float targetSpeed = (isSprinting ? sprintSpeed : walkSpeed);

        Vector3 vel = rb.linearVelocity;
        Vector3 velH = new Vector3(vel.x, 0f, vel.z);

        if (isGrounded)
        {
            // Instant stop when no input -> zero out along-surface velocity, keep vertical
            if (!hasInput)
            {
                // Remove velocity along the ground plane (but keep the normal component)
                Vector3 v = rb.linearVelocity;
                Vector3 vAlong = Vector3.ProjectOnPlane(v, groundNormal);
                rb.linearVelocity = v - vAlong; // leaves only the component along groundNormal
                                                // Optional: small slide if standing on steep slope
                if (OnSteepSlope && slideOnSteep)
                {
                    Vector3 uphill = Vector3.ProjectOnPlane(Vector3.up, groundNormal).normalized;
                    Vector3 downslope = -uphill;
                    Vector3 slide = downslope * slideGravity * Time.fixedDeltaTime;
                    rb.AddForce(slide, ForceMode.VelocityChange);
                    // simple friction against slide
                    rb.linearVelocity *= (1f - slideFriction * Time.fixedDeltaTime);
                }
                return;
            }

            // --- Slope-aware desired direction ---
            // Start with input in world space
            Vector3 moveDirWorld = transform.TransformDirection(cachedInput).normalized;

            // Project desired motion onto the ground plane so we 'stick' to the surface
            Vector3 alongSurface = Vector3.ProjectOnPlane(moveDirWorld, groundNormal).normalized;

            // If slope limit is exceeded, block uphill component (prevent walking up)
            if (OnSteepSlope)
            {
                Vector3 uphill = Vector3.ProjectOnPlane(Vector3.up, groundNormal).normalized;   // steepest uphill
                float uphillComp = Vector3.Dot(alongSurface, uphill);
                if (uphillComp > 0f)
                {
                    // remove uphill component; leave sideways/downhill motion
                    alongSurface -= uphill * uphillComp;
                    if (alongSurface.sqrMagnitude > 1e-4f) alongSurface.Normalize();
                    else alongSurface = Vector3.zero;
                }

                // Optional slide down when on steep slope
                if (slideOnSteep)
                {
                    Vector3 downslope = -uphill;
                    Vector3 slide = downslope * slideGravity * Time.fixedDeltaTime;
                    rb.AddForce(slide, ForceMode.VelocityChange);
                    rb.linearVelocity *= (1f - slideFriction * Time.fixedDeltaTime);
                }
            }

            // Target velocity along the ground plane (respecting slope)
            Vector3 targetVelAlong = alongSurface * targetSpeed;

            // Current velocity along the ground plane
            Vector3 velAlong = Vector3.ProjectOnPlane(rb.linearVelocity, groundNormal);

            // Delta we want to apply (also along plane)
            Vector3 delta = targetVelAlong - velAlong;

            // Clamp the "impulse" magnitude per tick (VelocityChange is in m/s)
            if (delta.sqrMagnitude > 0f)
            {
                // Clamp by your maxVelocityChange but along the surface
                delta = Vector3.ClampMagnitude(delta, maxVelocityChange);

                // Ensure we're not adding any component into the ground
                delta = Vector3.ProjectOnPlane(delta, groundNormal);

                rb.AddForce(delta, ForceMode.VelocityChange);
            }
        }

        else
        {
            // Airborne: preserve momentum when no input; gentle steer when there is
            if (!(preserveAirMomentum && !hasInput))
            {
                Vector3 targetVelH = wishDir * Mathf.Min(targetSpeed, velH.magnitude + airAcceleration);
                Vector3 delta = targetVelH - velH;
                delta = Vector3.ClampMagnitude(new Vector3(delta.x, 0f, delta.z), airMaxVelocityChange);
                rb.AddForce(new Vector3(delta.x, 0f, delta.z), ForceMode.VelocityChange);
            }
        }
    }






    private float groundedBufferUntil; // coyote time
    [SerializeField] float groundedSkin = 0.05f;
    [SerializeField] float coyoteTime = 0.06f;

    void CheckGround()
    {
        var col = GetComponent<CapsuleCollider>();
        float radius = col ? Mathf.Max(0.01f, col.radius * 0.95f) : 0.3f;
        Vector3 origin = transform.position + Vector3.up * (radius + 0.02f);
        float castDist = (col ? (col.height * 0.5f) : 0.9f) + groundedSkin;

        RaycastHit hit;
        bool didHit = Physics.SphereCast(origin, radius, Vector3.down, out hit, castDist, ~0, QueryTriggerInteraction.Ignore);
        if (didHit)
        {
            isGrounded = true;
            groundedBufferUntil = Time.time + coyoteTime;

            groundNormal = hit.normal;
            groundPoint = hit.point;
            groundAngle = Vector3.Angle(groundNormal, Vector3.up);
        }
        else
        {
            isGrounded = Time.time < groundedBufferUntil;

            // Keep last known normal when buffering; otherwise reset to up
            if (!isGrounded)
            {
                groundNormal = Vector3.up;
                groundAngle = 0f;
                groundPoint = transform.position;
            }
        }
    }



    private void Jump()
    {
        // Adds force to the player rigidbody to jump
        if (isGrounded)
        {
            rb.AddForce(0f, jumpPower, 0f, ForceMode.Impulse);
            isGrounded = false;
        }

        // When crouched and using toggle system, will uncrouch for a jump
        if(isCrouched && !holdToCrouch)
        {
            Crouch();
        }
    }

    private void Crouch()
    {
        // Stands player up to full height
        // Brings walkSpeed back up to original speed
        if(isCrouched)
        {
            transform.localScale = new Vector3(originalScale.x, originalScale.y, originalScale.z);
            walkSpeed /= speedReduction;

            isCrouched = false;
        }
        // Crouches player down to set height
        // Reduces walkSpeed
        else
        {
            transform.localScale = new Vector3(originalScale.x, crouchHeight, originalScale.z);
            walkSpeed *= speedReduction;

            isCrouched = true;
        }
    }

    private void HeadBob()
    {
        if(isWalking)
        {
            // Calculates HeadBob speed during sprint
            if(isSprinting)
            {
                timer += Time.deltaTime * (bobSpeed + sprintSpeed);
            }
            // Calculates HeadBob speed during crouched movement
            else if (isCrouched)
            {
                timer += Time.deltaTime * (bobSpeed * speedReduction);
            }
            // Calculates HeadBob speed during walking
            else
            {
                timer += Time.deltaTime * bobSpeed;
            }
            // Applies HeadBob movement
            joint.localPosition = new Vector3(jointOriginalPos.x + Mathf.Sin(timer) * bobAmount.x, jointOriginalPos.y + Mathf.Sin(timer) * bobAmount.y, jointOriginalPos.z + Mathf.Sin(timer) * bobAmount.z);
        }
        else
        {
            // Resets when play stops moving
            timer = 0;
            joint.localPosition = new Vector3(Mathf.Lerp(joint.localPosition.x, jointOriginalPos.x, Time.deltaTime * bobSpeed), Mathf.Lerp(joint.localPosition.y, jointOriginalPos.y, Time.deltaTime * bobSpeed), Mathf.Lerp(joint.localPosition.z, jointOriginalPos.z, Time.deltaTime * bobSpeed));
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!enableSwimming) return;
        if (other.CompareTag("Water") || ((waterLayer.value & (1 << other.gameObject.layer)) != 0))
        {
            // Try to infer surface height from the collider you entered
            currentWaterSurfaceY = float.NaN;
            if (waterSurface == null)
            {
                // If the trigger is an axis-aligned BoxCollider: top face is bounds.max.y
                if (other is BoxCollider)
                {
                    currentWaterSurfaceY = other.bounds.max.y;
                }
                else
                {
                    // Fallback: use object position (good if the water plane sits at its transform.y)
                    currentWaterSurfaceY = other.transform.position.y;
                }
            }

            BeginSwim();
        }

        /*if (other.CompareTag("EnemyWeapon"))
        {
            if (healthSystem != null)
            {
                other.GetComponent<Collider>().enabled = false;
                healthSystem.TakeDamage(other.GetComponentInParent<GruntEnemyAI>().Damage);
            }
        }*/
    }

    private void OnTriggerExit(Collider other)
    {
        if (!enableSwimming) return;
        if (other.CompareTag("Water") || ((waterLayer.value & (1 << other.gameObject.layer)) != 0))
        {
            EndSwim();
            currentWaterSurfaceY = float.NaN;
        }

        /*if (other.CompareTag("EnemyWeapon"))
        {
            other.enabled = true;
        }*/
    }


    private void BeginSwim()
    {
        if (isSwimming) return;
        isSwimming = true;

        isFalling = false;

        storedUseGravity = rb.useGravity;
        storedDrag = rb.linearDamping;           // if you use rb.drag, store that instead

        rb.useGravity = false;
        rb.linearDamping = waterDrag;

        // prevent sinking on enter
        Vector3 v = rb.linearVelocity;
        if (v.y < 0f) v.y = 0f;
        rb.linearVelocity = v;

        // Space is used to ascend—don’t trigger jump logic
        isZoomed = false;
    }

    private void EndSwim()
    {
        if (!isSwimming) return;
        isSwimming = false;

        rb.useGravity = storedUseGravity;
        rb.linearDamping = storedDrag;           // RESTORE what you had before water
    }

    /// <summary>
    /// Tracks airborne state and applies fall damage the frame we transition to grounded.
    /// Uses vertical distance from the highest point since leaving ground.
    /// </summary>
    private void HandleFallDamage()
    {
        if (!enableFallDamage || healthSystem == null) { wasGrounded = isGrounded; return; }

        // If we just left the ground, start tracking a new fall from our current height
        if (wasGrounded && !isGrounded)
        {
            // Start a new fall if we're actually moving downward or will be soon
            isFalling = true;
            fallStartY = transform.position.y;   // record the highest Y we were at when we stepped off
        }

        // While airborne, keep the highest Y as the reference (handles walking off small ledges after going up a ramp)
        if (!isGrounded && isFalling)
        {
            if (transform.position.y > fallStartY)
                fallStartY = transform.position.y;
        }

        // Landed this frame?
        if (!wasGrounded && isGrounded)
        {
            if (isFalling && !isSwimming) // ignore water entries
            {
                float fallDistance = fallStartY - transform.position.y;

                if (fallDistance > minFallHeight)
                {
                    // Linear damage past the safe threshold
                    float extra = fallDistance - minFallHeight;
                    int dmg = Mathf.CeilToInt(extra * damagePerExtraMeter);

                    // Optional lethal clamp at/above lethalFallHeight
                    if (fallDistance >= lethalFallHeight)
                    {
                        // Use a very large number; your CombatController should clamp to current HP
                        dmg = 99999;
                    }

                    // Crouch landing softens the blow
                    if (isCrouched && crouchDamageReduction > 0f)
                        dmg = Mathf.RoundToInt(dmg * (1f - crouchDamageReduction));

                    if (dmg > 0)
                        healthSystem.TakeDamage(dmg);
                }
            }

            // reset
            isFalling = false;
        }

        // update latched grounded state for next tick
        wasGrounded = isGrounded;
    }

}
