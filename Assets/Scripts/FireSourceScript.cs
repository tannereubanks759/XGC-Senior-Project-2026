using UnityEngine;
using UnityEngine.Animations;

[DisallowMultipleComponent]
public class FireSourceScript : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("If left empty, it will auto-find the first object tagged Player.")]
    public Transform player;

    [Header("Distance-Based Exponential Speed")]
    [Tooltip("Speed when far away (units/second).")]
    [Min(0f)] public float farSpeed = 0.5f;

    [Tooltip("Maximum speed cap (units/second).")]
    [Min(0f)] public float maxSpeed = 12f;

    [Tooltip("Distance (units) considered 'far'. Beyond this, speed stays near farSpeed.")]
    [Min(0.01f)] public float farDistance = 12f;

    [Tooltip("Distance (units) considered 'close'. At/inside this, speed will be near maxSpeed.")]
    [Min(0.01f)] public float closeDistance = 0.75f;

    [Tooltip("Controls how sharply speed increases as it gets closer. Bigger = more dramatic ramp.")]
    [Min(0.01f)] public float exponent = 3f;

    [Header("Stop / Arrival")]
    [Tooltip("Stops moving when see within this distance of player.")]
    [Min(0f)] public float stopDistance = 0.2f;

    [Header("Options")]
    [Tooltip("If true, keeps current Y and only moves in XZ (useful for ground souls).")]
    public bool lockY = false;

    [Tooltip("If true, rotates to face the movement direction.")]
    public bool faceMoveDirection = true;

    [Tooltip("How fast it rotates to face the movement direction (degrees/sec).")]
    [Min(0f)] public float turnSpeed = 720f;

    [Tooltip("Optional smoothing for speed changes (0 = none).")]
    [Min(0f)] public float speedSmoothing = 0.0f;


    private float _currentSpeed;

    public bool isCollected = false;
    public FireLamp lamp;
    public LookAtConstraint constraint;
    private void Awake()
    {
        lamp = GetComponentInParent<FireLamp>();
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }

        _currentSpeed = farSpeed;
    }

    private void Update()
    {

        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }

        if (player == null || !isCollected) return;

        Vector3 targetPos = player.position + new Vector3(0, 1f, 0);
        if (lockY) targetPos.y = transform.position.y;

        Vector3 toTarget = targetPos - transform.position;
        float dist = toTarget.magnitude;

        if (dist <= stopDistance) return;

        // Convert distance -> "closeness" in [0..1]
        // dist >= farDistance => closeness ~ 0
        // dist <= closeDistance => closeness ~ 1
        float denom = Mathf.Max(0.0001f, farDistance - closeDistance);
        float closeness = Mathf.Clamp01((farDistance - dist) / denom);

        // Exponential ramp based on closeness:
        // t = closeness^exponent, then speed lerps from farSpeed -> maxSpeed
        float t = Mathf.Pow(closeness, exponent);
        float desiredSpeed = Mathf.Lerp(farSpeed, maxSpeed, t);

        // Optional smoothing (helps avoid jittery speed changes if player moves a lot)
        if (speedSmoothing > 0f)
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, desiredSpeed, speedSmoothing * Time.deltaTime);
        else
            _currentSpeed = desiredSpeed;

        Vector3 dir = toTarget / dist; // normalized
        transform.position += dir * _currentSpeed * Time.deltaTime;

        if (faceMoveDirection)
        {
            Quaternion desiredRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRot, turnSpeed * Time.deltaTime);
        }
    }

    private void OnValidate()
    {
        // Keep distances sensible in inspector
        closeDistance = Mathf.Max(0.001f, closeDistance);
        farDistance = Mathf.Max(closeDistance + 0.001f, farDistance);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && isCollected)
        {
            other.GetComponentInChildren<FireballManager>().CollectFire();
            Destroy(this.gameObject);
        }
    }
}
