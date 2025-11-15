using RayFire;
using UnityEngine;
using UnityEngine.VFX;

public class EliteSkeleton : BaseEnemyAI
{
    [Header("Elite Specific Data")]
    [Tooltip("Controls how the enemy's attack effects the player")]
    private EliteType type;

    [Header("Swing VFX")]
    [Tooltip("The VFX to play during the swing")]
    [SerializeField] private VisualEffect swingEffect;
    [Tooltip("The Secondary VFX to play for the swing")]
    [SerializeField] private VisualEffect swingEffectTwo;

#pragma warning disable CS0114 // Member hides inherited member; missing override keyword
    private void Awake()
#pragma warning restore CS0114 // Member hides inherited member; missing override keyword
    {
        base.Awake();

        eliteType = type;

        PatrolArea area = FindClosestPatrolArea();
        RayfireRigid rf = GetComponentInChildren<RayfireRigid>();
        GameObject sword = GetComponentInChildren<Rigidbody>().gameObject;

        States[EnemyState.Idle] = new BasicIdleState(EnemyState.Idle, this);
        States[EnemyState.Patrol] = new BasicPatrolState(EnemyState.Patrol, this, area);
        States[EnemyState.Chase] = new BasicChaseState(EnemyState.Chase, this);
        States[EnemyState.Attack] = new BasicAttackState(EnemyState.Attack, this);
        States[EnemyState.Hit] = new BasicHitState(EnemyState.Hit, this);
        States[EnemyState.Dead] = new BasicDeadState(EnemyState.Dead, this, rf, sword);

        CurrentState = States[EnemyState.Idle];
    }

#pragma warning disable CS0114 // Member hides inherited member; missing override keyword
    private void Start()
#pragma warning restore CS0114 // Member hides inherited member; missing override keyword

    {
        base.Start();
    }

    public void PlaySwingEffect()
    {
        swingEffect.Play();
    }

    public void PlaySwingEffectTwo()
    {
        swingEffectTwo.Play();
    }
}

public enum EliteType
{
    Basic,
    Fire,
    Water,
    Gas,
}
