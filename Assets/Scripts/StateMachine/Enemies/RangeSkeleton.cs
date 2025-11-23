using RayFire;
using UnityEngine;

public class RangeSkeleton : BaseEnemyAI
{
    public ParticleSystem flintVFX;
    public ParticleSystem shotVFX;
    public GameObject gunPos;

    [SerializeField] private int damage = 10;
    [SerializeField] private float rayDistance = 25f;
    [SerializeField] private float sphereRadius = .5f;

    private Vector3 storedPlayerPos;


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

        // Aim using stored position
        Vector3 playerPos = storedPlayerPos;
        Vector3 originPos = origin.transform.position;

        // Horizontal facing (yaw)
        Vector3 flatForward = origin.transform.forward;
        flatForward.y = 0f;
        flatForward.Normalize();

        // Horizontal distance between gun and player
        Vector3 toPlayer = playerPos - originPos;
        float horizontalDist = new Vector2(toPlayer.x, toPlayer.z).magnitude;

        // Vertical difference
        float vertical = playerPos.y - originPos.y;

        // Build direction: same horizontal aim, correct vertical height
        Vector3 dir = (flatForward * horizontalDist + Vector3.up * vertical).normalized;

        Ray ray = new Ray(origin.transform.position, dir);

        Debug.DrawRay(origin.transform.position, dir * 10f, Color.red, 1f);

        if (Physics.SphereCast(ray, sphereRadius, out RaycastHit hit, rayDistance, playerMask))
        {

            if (!hit.collider.CompareTag("Player"))
                return;

            // Direction of hit
            Vector3 hitDir = (hit.collider.transform.position - origin.transform.position).normalized;

            playerController.TakeDamage(damage, hitDir);

            // curse logic
            var lantern = FindFirstObjectByType<curseOffhand>();

            if (lantern != null && lantern.reflectionUpgrade == true)
            {
                if (lantern.cursedEnemy == enemyAI)
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
    }
}