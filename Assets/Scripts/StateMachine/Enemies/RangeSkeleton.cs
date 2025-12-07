using RayFire;
using UnityEngine;

public class RangeSkeleton : BaseEnemyAI
{
    public ParticleSystem flintVFX;
    public ParticleSystem shotVFX;
    public GameObject gunPos;

    [SerializeField] private int damage = 10;
    [SerializeField] private float rayDistance = 50f;
    [SerializeField] private float sphereRadius = .5f;

    private Vector3 storedPlayerPos;

    public AudioClip sizzle;
    public AudioClip gunShot;

    public AudioSource audioSource;

    public GameObject bulletTrail;


#pragma warning disable CS0114 // Member hides inherited member; missing override keyword
    private void Awake()
#pragma warning restore CS0114 // Member hides inherited member; missing override keyword
    {
        base.Awake();

        PatrolArea area = FindClosestPatrolArea();
        RayfireRigid rayf = GetComponentInChildren<RayfireRigid>();
        GameObject gun = GetComponentInChildren<Rigidbody>().gameObject;

        States[EnemyState.Idle] = new BasicIdleState(EnemyState.Idle, this);
        if (canMove)
        {
            States[EnemyState.Patrol] = new BasicPatrolState(EnemyState.Patrol, this, area);
            States[EnemyState.Chase] = new BasicChaseState(EnemyState.Chase, this);
        }
        States[EnemyState.Attack] = new RangeAttackState(EnemyState.Attack, this);
        States[EnemyState.Dead] = new BasicDeadState(EnemyState.Dead, this, rayf, gun);

        CurrentState = States[EnemyState.Idle];
    }

    public void FlintVFX()
    {
        flintVFX.Play();
    }

    public void ShotVFX()
    { 
        shotVFX.Play(); 
    }
    public void CachePlayerPosition()
    {
        if (playerController != null)
            storedPlayerPos = playerController.transform.position;
    }


    private void ShootRay(GameObject origin, BaseEnemyAI enemyAI = null)
    {
        Vector3 originPos = origin.transform.position;

        // Build direction (same as yours)
        Vector3 playerPos = storedPlayerPos;
        Vector3 flatForward = origin.transform.forward;
        flatForward.y = 0f;
        flatForward.Normalize();

        Vector3 toPlayer = playerPos - originPos;
        float horizontalDist = new Vector2(toPlayer.x, toPlayer.z).magnitude;
        float vertical = playerPos.y - originPos.y;

        Vector3 dir = (flatForward * horizontalDist + Vector3.up * vertical).normalized;

        // ---------------------------------------------------------
        // 1) FIRST SPHERECAST: VISUAL HIT (NO MASK)
        // ---------------------------------------------------------
        bool visualHit = Physics.SphereCast(
            originPos,
            sphereRadius,
            dir,
            out RaycastHit visualInfo,
            rayDistance
        );

        // Determine endPos for the trail
        Vector3 endPos = visualHit ? visualInfo.point : originPos + dir * rayDistance;

        // Spawn trail ALWAYS
        GameObject trail = Instantiate(bulletTrail);
        trail.GetComponent<InstantBulletTrail>().Initialize(originPos, endPos);

        // ---------------------------------------------------------
        // 2) SECOND SPHERECAST: PLAYER HIT CHECK (MASKED)
        // ---------------------------------------------------------
        if (Physics.SphereCast(
            originPos,
            sphereRadius,
            dir,
            out RaycastHit hit,
            rayDistance,
            playerMask))
        {
            if (hit.collider.CompareTag("Player"))
            {
                // Apply damage
                Vector3 hitDir = (hit.collider.transform.position - originPos).normalized;
                playerController.TakeDamage(damage, hitDir);

                // Curse logic
                var lantern = FindFirstObjectByType<curseOffhand>();
                if (lantern != null && lantern.reflectionUpgrade && lantern.cursedEnemy == enemyAI)
                {
                    enemyAI.TakeDamage(5);
                }
            }
        }
    }


    public void Shoot()
    {
        ShotVFX();
        ShootRay(gunPos);
        audioSource.PlayOneShot(gunShot, 10f);
    }

    public void StartSizzle()
    {
        audioSource.Play();
    }

    public void EndSizzle()
    {
        audioSource.Stop();
    }
}