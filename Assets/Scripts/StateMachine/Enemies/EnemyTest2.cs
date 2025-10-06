using UnityEngine;

public class EnemyTest2 : BaseEnemyAI
{
    [Header("Prototype Enemy Combat Values")]
    [Tooltip("The minimum amount of time that can be spent blocking")]
    public float minBlockTime = 2.5f;
    [Tooltip("The maximum amount of time that can be spent blocking")]
    public float maxBlockTime = 3.5f;
    private float blockTime;

    void Awake()
    {
        base.Awake();

        PatrolArea area = FindClosestPatrolArea();

        Collider sword = GetComponentInChildren<AffectPlayer>().swordCollider;

        States[EnemyState.Idle] = new IdleState(EnemyState.Idle, this);
        States[EnemyState.Patrol] = new PatrolState(EnemyState.Patrol, this, area);
        States[EnemyState.Run] = new RunState(EnemyState.Run, this);
        States[EnemyState.Combat] = new CombatState(EnemyState.Combat, this);
        States[EnemyState.Attack] = new AttackState(EnemyState.Attack, this);
        States[EnemyState.Dead] = new DeadState(EnemyState.Dead, this);
        States[EnemyState.Hit] = new HitState(EnemyState.Hit, this);

        CurrentState = States[EnemyState.Idle];
    }

    #region Init Methods
    // Init the blocking time
    private void BlockTimeInit()
    {
        blockTime = CalcBlockTime();
    }

    #endregion

    #region Math Methods
    // Get the full second of the float
    private int GetFullSecond(float time)
    {
        var tempTime = Mathf.FloorToInt(time);

        return tempTime;
    }

    // Get the millisecond
    private int GetMillisecond(float time)
    {
        // Take full seconds off until there isn't one left
        while (time >= 1.0f)
        {
            time -= 1.0f;
        }

        // Multiply by 10 to get a whole number
        time = time * 10f;

        // Floor to get rid of decimal
        var tempTime = Mathf.FloorToInt(time);

        return tempTime;
    }

    private float BlockTimeIntToFloat(int second, int millisecond)
    {
        float tempTime = 0f;

        for (int i = 0; i < second; i++)
        {
            tempTime += 1.0f;
        }

        for (int i = 0; i < millisecond; i++)
        {
            tempTime += 0.1f;
        }

        return tempTime;
    }

    // Calc the blocking time
    private float CalcBlockTime()
    {
        int blockSec;
        int blockMillisec;

        blockSec = Random.Range(GetFullSecond(minBlockTime), GetFullSecond(maxBlockTime) + 1);

        if (blockSec < GetFullSecond(maxBlockTime))
        {
            blockMillisec = Random.Range(GetMillisecond(minBlockTime), 10);
        }
        else
        {
            blockMillisec = Random.Range(0, GetMillisecond(maxBlockTime) + 1);
        }

        return BlockTimeIntToFloat(blockSec, blockMillisec);
    }
    #endregion
}
