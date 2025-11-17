using RayFire;
using UnityEngine;

public class RangeSkeleton : BaseEnemyAI
{
    public ParticleSystem flintVFX;
    public ParticleSystem shotVFX;
    public GameObject gunPos;

    [SerializeField] private int damage = 10;
    [SerializeField] private float rayDistance = 25f;

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
        
        // Aim using the stored position from the anim event
        storedPlayerPos = playerController.transform.position;
        //storedPlayerPos += new Vector3(0, .5f, 0);
        Vector3 toPlayer = storedPlayerPos - origin.transform.position;

        // Only correct vertical aim so player can dodge horizontally
        Vector3 forward = origin.transform.forward;
        forward.y = toPlayer.normalized.y;

        Vector3 dir = forward.normalized;

        Ray ray = new Ray(origin.transform.position, dir);
        
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, playerMask))
        {
            Debug.DrawRay(origin.transform.position, origin.transform.forward * 100f, Color.red, 1f);

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