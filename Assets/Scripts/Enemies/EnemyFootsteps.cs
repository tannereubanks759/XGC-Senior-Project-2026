using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyFootsteps : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource soundSource;
    public AudioClip[] footstepClips;
    [Range(0f, 1f)] public float volume = 0.9f;

    [Header("Intervals")]
    [Min(0.05f)] public float walkFootstepInterval = 0.55f;
    [Min(0.05f)] public float runFootstepInterval = 0.32f;

    [Header("Speed Mapping")]
    [Min(0.01f)] public float walkSpeed = 3.5f;
    [Min(0.01f)] public float runSpeed = 6.0f;

    [Header("Movement Gates")]
    [Min(0f)] public float minMoveVelocity = 0.15f;
    public bool requireOnNavMesh = true;

    [HideInInspector] public bool footstepsAllowed = false;

    private NavMeshAgent agent;
    private float nextStepTime;

    // Optional global limiter to avoid many PlayOneShot calls on same frame
    private static int lastStepFrame = -1;

    private bool _registered;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!soundSource) soundSource = GetComponent<AudioSource>();
        nextStepTime = Time.time + Random.Range(0f, 0.2f);
    }

    private void OnEnable()
    {
        RegisterOnce();
    }

    private void OnDisable()
    {
        UnregisterOnce();
    }

    private void RegisterOnce()
    {
        if (_registered) return;
        _registered = true;

        // If the manager might not exist in scene yet, guard it:
        if (EnemyFootstepsManager.InstanceExists)
            EnemyFootstepsManager.Register(this);
    }

    private void UnregisterOnce()
    {
        if (!_registered) return;
        _registered = false;

        if (EnemyFootstepsManager.InstanceExists)
            EnemyFootstepsManager.Unregister(this);
    }

    private void Update()
    {
        if (!footstepsAllowed) return;
        if (agent == null || soundSource == null) return;
        if (footstepClips == null || footstepClips.Length == 0) return;

        if (requireOnNavMesh && !agent.isOnNavMesh) return;
        if (agent.isStopped) return;
        if (agent.velocity.magnitude < minMoveVelocity) return;
        if (Time.time < nextStepTime) return;

        float t = Mathf.InverseLerp(walkSpeed, runSpeed, agent.speed);
        float interval = Mathf.Lerp(walkFootstepInterval, runFootstepInterval, t);

        if (lastStepFrame == Time.frameCount)
        {
            nextStepTime = Time.time + 0.01f;
            return;
        }

        int idx = Random.Range(0, footstepClips.Length);
        soundSource.volume = volume;
        soundSource.spatialBlend = 1f;
        soundSource.PlayOneShot(footstepClips[idx]);

        lastStepFrame = Time.frameCount;
        nextStepTime = Time.time + interval;
    }
}