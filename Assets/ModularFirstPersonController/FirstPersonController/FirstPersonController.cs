// CHANGE LOG
//
// CHANGES || version VERSION
//
// "Enable/Disable Headbob, Changed look rotations - should result in reduced camera jitters" || version 1.0.1
//
// Sprint system replaced with Slider-based stamina + sprint lock until 50% regen || version 1.0.2

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

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

    public float speed { get; private set; }

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

    #region Sprint (Slider-based Stamina)

    [Header("Sprint / Stamina (Slider)")]
    public bool enableSprint = true;
    public bool unlimitedSprint = false;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public float sprintSpeed = 7f;

    [Tooltip("Max stamina (think of this as 'seconds' of sprint if drain rate is 1).")]
    public float maxStamina = 5f;

    [Tooltip("Stamina drained per second while sprinting.")]
    public float staminaDrainPerSecond = 1f;

    [Tooltip("Stamina regenerated per second while NOT sprinting.")]
    public float staminaRegenPerSecond = 1f;

    [Tooltip("Optional delay after any stamina use (sprint or LoseStamina) before regen starts.")]
    public float regenDelay = 0.15f;

    [Tooltip("If stamina hits 0, player must regen to this % of max before sprint is allowed again.")]
    [Range(0f, 1f)]
    public float sprintUnlockPercent = 0.5f;

    public float sprintFOV = 80f;
    public float sprintFOVStepTime = 10f;

    [Header("Stamina UI")]
    [Tooltip("Assign your Canvas Slider here.")]
    public Slider staminaSlider;

    [Tooltip("Optional root GameObject to hide/show (if null, uses the slider GameObject).")]
    public GameObject staminaUIRoot;

    [Tooltip("Hide the stamina UI when full.")]
    public bool hideUIWhenFull = true;
    [Header("Stamina UI Colors")]
    [Tooltip("Image used as the slider Fill (usually: StaminaSlider/Fill Area/Fill). If left null, we'll try to find it automatically.")]
    public RawImage staminaFillImage;

    [Tooltip("Color when sprint is available.")]
    public Color staminaNormalColor = Color.white;

    [Tooltip("Color when stamina is empty / sprint is locked.")]
    public Color staminaLockedColor = new Color(0.55f, 0.55f, 0.55f, 1f); // gray

    // Internal
    private bool _staminaWasLockedVisual = false;
    // Internal
    private bool isSprinting = false;
    public float stamina;
    private bool sprintLocked = false;
    private float lastStaminaUseTime = -999f;
    UImanager UIM;
    #endregion

    #region Jump

    public bool enableJump = true;
    public KeyCode jumpKey = KeyCode.Space;
    public float jumpPower = 5f;

    // Internal Variables
    public bool isGrounded = false;
    private bool GroundHitThisFrame = false;
    private bool RawGrounded = false;

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

    [Tooltip("No damage until you fall farther than this (meters).")]
    public float minFallHeight = 3.0f;

    [Tooltip("At/above this height the impact is lethal.")]
    public float lethalFallHeight = 18f;

    [Tooltip("Linear damage beyond minFallHeight (damage per extra meter).")]
    public int damagePerExtraMeter = 10;

    [Range(0f, 1f)]
    public float crouchDamageReduction = 0.2f;

    // --- Internals for fall tracking ---
    private bool wasGrounded = false;
    private bool isFalling = false;
    private float fallStartY = 0f;

    // --- Ladder Climbing (MC-style: no snapping/teleporting) ---
    [Header("Ladders")]
    public bool enableLadders = true;
    [Tooltip("Up/down climb speed (m/s).")]
    public float ladderClimbSpeed = 3.5f;
    [Tooltip("Max downward slide when idle (m/s).")]
    public float ladderIdleSlideSpeed = 1.25f;
    [Tooltip("Optional: Space to jump off gives a tiny upward impulse.")]
    public float ladderJumpUpImpulse = 3.5f;

    [Header("Loading Screen")]
    public GameObject loadingScreen;

    private bool isOnLadder = false;
    private Transform currentLadder = null;

    #endregion
    
    private void Awake()
    {
        SceneManager.sceneLoaded += SceneLoaded;
        rb = GetComponent<Rigidbody>();
        loadingScreen.SetActive(false);
        crosshairObject = GetComponentInChildren<Image>();

        // Set internal variables
        playerCamera.fieldOfView = fov;
        originalScale = transform.localScale;
        jointOriginalPos = joint.localPosition;

        // Stamina init
        stamina = Mathf.Max(0.01f, maxStamina);
        UIM = GetComponentInChildren<UImanager>();
    }

    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (this.gameObject != null)
        {
            if (UIM) UIM.CloseWinScreen(); UIM.OpenPlayerUIScreen();
            loadingScreen.SetActive(false);
            SkeletonSpawnManager spawnmg = GameObject.FindAnyObjectByType<SkeletonSpawnManager>();
            if (spawnmg != null)
            {
                spawnmg.playerPos = this.gameObject.transform;
            }

        }
    }

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        loadingScreen.SetActive(false);

        if (lockCursor)
            Cursor.lockState = CursorLockMode.Locked;

        // Try to auto-find the slider fill image if not assigned
        if (staminaFillImage == null && staminaSlider != null)
        {
            var fill = staminaSlider.fillRect;
            if (fill != null) staminaFillImage = fill.GetComponent<RawImage>();
        }

        // Initialize to normal color on start
        if (staminaFillImage != null)
            staminaFillImage.color = staminaNormalColor;

        GameObject waterObject = GameObject.FindGameObjectWithTag("Water");
        if (waterObject != null) waterSurface = waterObject.gameObject.transform;
        else waterSurface = this.gameObject.transform;

        // Stamina UI init
        if (staminaUIRoot == null && staminaSlider != null)
            staminaUIRoot = staminaSlider.gameObject;

        if (staminaSlider != null)
        {
            staminaSlider.minValue = 0f;
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = stamina;
        }
        UpdateStaminaUI();

        healthSystem = GetComponentInChildren<CombatController>();
    }

    float camRotation;

    private void Update()
    {
        #region Camera
        if (isPaused) return;

        if (cameraCanMove)
        {
            yaw = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * mouseSensitivity;

            if (!invertCamera) pitch -= mouseSensitivity * Input.GetAxis("Mouse Y");
            else pitch += mouseSensitivity * Input.GetAxis("Mouse Y");

            pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

            transform.localEulerAngles = new Vector3(0, yaw, 0);
            playerCamera.transform.localEulerAngles = new Vector3(pitch, 0, 0);
        }

        #region Camera Zoom
        if (enableZoom)
        {
            if (Input.GetKeyDown(zoomKey) && !holdToZoom && !isSprinting)
                isZoomed = !isZoomed;

            if (holdToZoom && !isSprinting)
            {
                if (Input.GetKeyDown(zoomKey)) isZoomed = true;
                else if (Input.GetKeyUp(zoomKey)) isZoomed = false;
            }

            if (isZoomed)
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, zoomFOV, zoomStepTime * Time.deltaTime);
            else if (!isZoomed && !isSprinting)
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, fov, zoomStepTime * Time.deltaTime);
        }
        #endregion
        #endregion

        // Cache input for both UI logic and physics step
        cachedInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxis("Vertical"));
        cachedHasInput = cachedInput.sqrMagnitude > 0.0001f;

        #region Sprint (New Slider Stamina)
        HandleSprintAndStamina();
        #endregion

        #region Jump
        if (enableJump && Input.GetKeyDown(jumpKey))
        {
            if (isOnLadder)
            {
                rb.AddForce(Vector3.up * ladderJumpUpImpulse, ForceMode.Impulse);
                EndLadder();
            }
            else if (!isSwimming && RawGrounded)
            {
                Jump();
            }
        }
        #endregion

        #region Crouch
        if (enableCrouch)
        {
            if (Input.GetKeyDown(crouchKey) && !holdToCrouch)
            {
                Crouch();
            }

            if (Input.GetKeyDown(crouchKey) && holdToCrouch)
            {
                isCrouched = false;
                Crouch();
            }
            else if (Input.GetKeyUp(crouchKey) && holdToCrouch)
            {
                isCrouched = true;
                Crouch();
            }
        }
        #endregion

        CheckGround();
        HandleFallDamage();

        if (enableHeadBob && !isSwimming)
            HeadBob();

        isWalking = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).sqrMagnitude > 0.01f;
    }

    private void HandleSprintAndStamina()
    {
        if (!enableSprint)
        {
            isSprinting = false;
            return;
        }

        // Unlock sprint once stamina reaches threshold after depletion
        if (!unlimitedSprint && sprintLocked && stamina >= maxStamina * sprintUnlockPercent)
            sprintLocked = false;

        bool wantsSprint =
            Input.GetKey(sprintKey) &&
            cachedHasInput &&
            !sprintLocked &&
            (unlimitedSprint || stamina > 0.0001f);

        isSprinting = wantsSprint;

        if (isSprinting)
        {
            // Sprint FOV
            isZoomed = false;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, sprintFOV, sprintFOVStepTime * Time.deltaTime);

            // Drain stamina
            if (!unlimitedSprint)
            {
                stamina -= staminaDrainPerSecond * Time.deltaTime;
                lastStaminaUseTime = Time.time;

                if (stamina <= 0f)
                {
                    stamina = 0f;
                    isSprinting = false;
                    sprintLocked = true; // lock until we regen to unlock percent
                }
            }
        }
        else
        {
            // Return FOV to normal (unless zoom is active)
            if (!isZoomed)
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, fov, sprintFOVStepTime * Time.deltaTime);

            // Regen stamina (after delay)
            if (!unlimitedSprint)
            {
                if (Time.time >= lastStaminaUseTime + regenDelay)
                    stamina = Mathf.Clamp(stamina + staminaRegenPerSecond * Time.deltaTime, 0f, maxStamina);
            }
        }

        UpdateStaminaUI();
    }

    private void UpdateStaminaUI()
    {
        if (unlimitedSprint)
        {
            if (staminaSlider != null) staminaSlider.value = maxStamina;
            if (staminaUIRoot != null && hideUIWhenFull) staminaUIRoot.SetActive(false);
            return;
        }

        if (staminaSlider != null)
        {
            // Keep max synced if you tweak in inspector at runtime
            staminaSlider.maxValue = Mathf.Max(0.01f, maxStamina);
            staminaSlider.value = stamina;

            // --- Color switching: gray when sprint can't be used ---
            bool shouldLookLocked =
                !unlimitedSprint &&
                enableSprint &&
                (sprintLocked || stamina <= 0.0001f);

            if (staminaFillImage != null && _staminaWasLockedVisual != shouldLookLocked)
            {
                staminaFillImage.color = shouldLookLocked ? staminaLockedColor : staminaNormalColor;
                _staminaWasLockedVisual = shouldLookLocked;
            }
        }

        if (staminaUIRoot != null)
        {
            if (hideUIWhenFull)
                staminaUIRoot.SetActive(stamina < maxStamina - 0.0001f);
            else
                staminaUIRoot.SetActive(true);
        }
    }

    /// <summary>
    /// Call this from attacks/abilities/etc. Amount is in the same units as stamina (by default: "seconds" worth).
    /// Example: LoseStamina(1.25f) removes 1.25 stamina.
    /// If stamina hits 0, sprint locks until stamina regenerates to sprintUnlockPercent.
    /// </summary>
    public void LoseStamina(float amount)
    {
        if (unlimitedSprint) return;
        if (amount <= 0f) return;

        stamina = Mathf.Max(0f, stamina - amount);
        lastStaminaUseTime = Time.time;

        if (stamina <= 0f)
            sprintLocked = true;

        UpdateStaminaUI();
    }

    void FixedUpdate()
    {
        if (isPaused) return;
        if (!playerCanMove) return;

        // --- LADDER MOVEMENT ---
        if (enableLadders && isOnLadder && currentLadder != null)
        {
            float v = cachedInput.z; // W/S
            Vector3 vel1 = rb.linearVelocity;

            if (v > 0.01f) vel1.y = ladderClimbSpeed;
            else if (v < -0.01f) vel1.y = -ladderClimbSpeed;
            else
            {
                if (vel1.y < -ladderIdleSlideSpeed)
                    vel1.y = -ladderIdleSlideSpeed;
            }

            rb.linearVelocity = vel1;
            return;
        }

        // --- SWIMMING ---
        if (isSwimming && enableSwimming)
        {
            float camY = playerCamera.transform.position.y;

            float waterY = !float.IsNaN(currentWaterSurfaceY)
                ? currentWaterSurfaceY
                : (waterSurface != null ? waterSurface.position.y : transform.position.y + 99999f);

            if (playerCamera != null)
            {
                if (camY > waterY - submergeOffset && !Input.GetKey(swimUpKey))
                {
                    rb.AddForce(Vector3.down * sinkAcceleration, ForceMode.Acceleration);
                }
            }

            Transform camT = playerCamera != null ? playerCamera.transform : transform;
            float h = cachedInput.x;
            float v = cachedInput.z;

            Vector3 fwd = camT.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = camT.right; right.y = 0f; right.Normalize();

            Vector3 horiz = (right * h + fwd * v);
            if (horiz.sqrMagnitude > 1f) horiz.Normalize();

            float upInput = 0f;
            float surfaceY = currentWaterSurfaceY;
            if (waterSurface != null) surfaceY = waterSurface.position.y;

            if (Input.GetKey(swimUpKey) && camY < surfaceY - submergeOffset) upInput += 1f;
            if (Input.GetKey(swimDownKey)) upInput -= 1f;

            Vector3 vert = camT.up * upInput;

            Vector3 swimAccel = horiz * swimSpeed + vert * swimUpForce;

            // Optional: sprint boost in water uses same gating as sprint availability
            bool canBoost = enableSprint && Input.GetKey(sprintKey) && !sprintLocked && (unlimitedSprint || stamina > 0.0001f);
            if (canBoost) swimAccel *= 1.15f;

            rb.AddForce(swimAccel, ForceMode.Acceleration);
            return;
        }

        // --- LAND/AIR MOVEMENT ---
        Vector3 wishDir = transform.TransformDirection(cachedInput).normalized;
        bool hasInput = cachedHasInput;

        float targetSpeed = (isSprinting ? sprintSpeed : walkSpeed);

        Vector3 vel = rb.linearVelocity;
        Vector3 velH = new Vector3(vel.x, 0f, vel.z);

        if (isGrounded)
        {
            if (!hasInput)
            {
                Vector3 v = rb.linearVelocity;
                Vector3 vAlong = Vector3.ProjectOnPlane(v, groundNormal);
                rb.linearVelocity = v - vAlong;

                if (OnSteepSlope && slideOnSteep)
                {
                    Vector3 uphill = Vector3.ProjectOnPlane(Vector3.up, groundNormal).normalized;
                    Vector3 downslope = -uphill;
                    Vector3 slide = downslope * slideGravity * Time.fixedDeltaTime;
                    rb.AddForce(slide, ForceMode.VelocityChange);
                    rb.linearVelocity *= (1f - slideFriction * Time.fixedDeltaTime);
                }
                return;
            }

            Vector3 moveDirWorld = transform.TransformDirection(cachedInput).normalized;
            Vector3 alongSurface = Vector3.ProjectOnPlane(moveDirWorld, groundNormal).normalized;

            if (OnSteepSlope)
            {
                Vector3 uphill = Vector3.ProjectOnPlane(Vector3.up, groundNormal).normalized;
                float uphillComp = Vector3.Dot(alongSurface, uphill);
                if (uphillComp > 0f)
                {
                    alongSurface -= uphill * uphillComp;
                    if (alongSurface.sqrMagnitude > 1e-4f) alongSurface.Normalize();
                    else alongSurface = Vector3.zero;
                }

                if (slideOnSteep)
                {
                    Vector3 downslope = -uphill;
                    Vector3 slide = downslope * slideGravity * Time.fixedDeltaTime;
                    rb.AddForce(slide, ForceMode.VelocityChange);
                    rb.linearVelocity *= (1f - slideFriction * Time.fixedDeltaTime);
                }
            }

            Vector3 targetVelAlong = alongSurface * targetSpeed;
            Vector3 velAlong = Vector3.ProjectOnPlane(rb.linearVelocity, groundNormal);
            Vector3 delta = targetVelAlong - velAlong;

            if (delta.sqrMagnitude > 0f)
            {
                delta = Vector3.ClampMagnitude(delta, maxVelocityChange);
                delta = Vector3.ProjectOnPlane(delta, groundNormal);
                rb.AddForce(delta, ForceMode.VelocityChange);
            }
        }
        else
        {
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
        float radius = Mathf.Max(0.01f, col.radius * 0.95f);

        Vector3 center = transform.TransformPoint(col.center);
        Vector3 origin = center + Vector3.up * 0.02f;
        float castDist = (col.height * 0.5f) - radius + groundedSkin;

        RaycastHit hit;
        bool didHit = Physics.SphereCast(origin, radius, Vector3.down, out hit, castDist, ~0, QueryTriggerInteraction.Ignore);

        RawGrounded = didHit;
        GroundHitThisFrame = didHit;

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
        }
    }

    private void Jump()
    {
        if (isGrounded)
        {
            rb.AddForce(0f, jumpPower, 0f, ForceMode.Impulse);
            isGrounded = false;
        }

        if (isCrouched && !holdToCrouch)
            Crouch();
    }

    private void Crouch()
    {
        if (isCrouched)
        {
            transform.localScale = new Vector3(originalScale.x, originalScale.y, originalScale.z);
            walkSpeed /= speedReduction;
            isCrouched = false;
        }
        else
        {
            transform.localScale = new Vector3(originalScale.x, crouchHeight, originalScale.z);
            walkSpeed *= speedReduction;
            isCrouched = true;
        }
    }

    private void HeadBob()
    {
        if (isWalking)
        {
            if (isSprinting) timer += Time.deltaTime * (bobSpeed + sprintSpeed);
            else if (isCrouched) timer += Time.deltaTime * (bobSpeed * speedReduction);
            else timer += Time.deltaTime * bobSpeed;

            joint.localPosition = new Vector3(
                jointOriginalPos.x + Mathf.Sin(timer) * bobAmount.x,
                jointOriginalPos.y + Mathf.Sin(timer) * bobAmount.y,
                jointOriginalPos.z + Mathf.Sin(timer) * bobAmount.z
            );
        }
        else
        {
            timer = 0;
            joint.localPosition = new Vector3(
                Mathf.Lerp(joint.localPosition.x, jointOriginalPos.x, Time.deltaTime * bobSpeed),
                Mathf.Lerp(joint.localPosition.y, jointOriginalPos.y, Time.deltaTime * bobSpeed),
                Mathf.Lerp(joint.localPosition.z, jointOriginalPos.z, Time.deltaTime * bobSpeed)
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (enableLadders && other.CompareTag("ladder"))
        {
            BeginLadder(other);
            return;
        }

        if (!enableSwimming) return;
        if (other.CompareTag("Water") || ((waterLayer.value & (1 << other.gameObject.layer)) != 0))
        {
            currentWaterSurfaceY = float.NaN;
            if (waterSurface == null)
                currentWaterSurfaceY = (other is BoxCollider) ? other.bounds.max.y : other.transform.position.y;
            BeginSwim();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("ladderTop") && currentLadder != null)
        {
            currentLadder.GetComponent<BoxCollider>().isTrigger = false;
        }
        if (enableLadders && other.CompareTag("ladder"))
        {
            if (currentLadder != null && other.transform == currentLadder)
                EndLadder();
            return;
        }

        if (!enableSwimming) return;
        if (other.CompareTag("Water") || ((waterLayer.value & (1 << other.gameObject.layer)) != 0))
        {
            EndSwim();
            currentWaterSurfaceY = float.NaN;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("ladderTop") && currentLadder != null)
        {
            currentLadder.GetComponent<BoxCollider>().isTrigger = true;
        }
    }

    private void BeginSwim()
    {
        if (isSwimming) return;
        isSwimming = true;

        isFalling = false;

        storedUseGravity = rb.useGravity;
        storedDrag = rb.linearDamping;

        rb.useGravity = false;
        rb.linearDamping = waterDrag;

        Vector3 v = rb.linearVelocity;
        if (v.y < 0f) v.y = 0f;
        rb.linearVelocity = v;

        isZoomed = false;
    }

    private void EndSwim()
    {
        if (!isSwimming) return;
        isSwimming = false;

        rb.useGravity = storedUseGravity;
        rb.linearDamping = storedDrag;
    }

    private void BeginLadder(Collider ladderCol)
    {
        if (!enableLadders || isOnLadder) return;
        isOnLadder = true;
        currentLadder = ladderCol.transform;

        isFalling = false;
        fallStartY = transform.position.y;
    }

    private void EndLadder()
    {
        if (!isOnLadder) return;
        isOnLadder = false;
        currentLadder = null;

        isFalling = false;
        fallStartY = transform.position.y;

        groundedBufferUntil = Time.time + coyoteTime;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
    }

    private void HandleFallDamage()
    {
        if (!enableFallDamage || healthSystem == null) { wasGrounded = isGrounded; return; }
        if (isOnLadder)
        {
            isFalling = false;
            fallStartY = transform.position.y;
            wasGrounded = isGrounded;
            return;
        }

        if (wasGrounded && !isGrounded)
        {
            isFalling = true;
            fallStartY = transform.position.y;
        }

        if (!isGrounded && isFalling)
        {
            if (transform.position.y > fallStartY)
                fallStartY = transform.position.y;
        }

        if (!wasGrounded && isGrounded)
        {
            if (isFalling && !isSwimming)
            {
                float fallDistance = fallStartY - transform.position.y;

                if (fallDistance > minFallHeight)
                {
                    float extra = fallDistance - minFallHeight;
                    int dmg = Mathf.CeilToInt(extra * damagePerExtraMeter);

                    if (fallDistance >= lethalFallHeight)
                        dmg = 99999;

                    if (isCrouched && crouchDamageReduction > 0f)
                        dmg = Mathf.RoundToInt(dmg * (1f - crouchDamageReduction));

                    if (dmg > 0)
                        healthSystem.TakeDamage(dmg);
                }
            }

            isFalling = false;
        }

        wasGrounded = isGrounded;
    }
}
