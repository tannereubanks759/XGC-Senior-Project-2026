/*
 * GruntEnemyAI.cs
 * 
 * This script defines a specific type of enemy: the "Grunt".
 * It inherits from BaseEnemyAI and initializes all the FSM states
 * for this enemy type. The Grunt starts patrolling in the closest
 * PatrolArea and has access to all base behaviors like Attack, Chase,
 * Hit, Block, BackDodge, and Dead.
 * 
 * By: Matthew Bolger
*/

using UnityEngine;

public class GruntEnemyAI : BaseEnemyAI
{
    [Header("Prototype Enemy Combat Values")]
    [Tooltip("The minimum amount of time that can be spent blocking")]
    public float minBlockTime = 2.5f;
    [Tooltip("The maximum amount of time that can be spent blocking")]
    public float maxBlockTime = 3.5f;
    private float blockTime;

    #region Monobehavior Methods
    // Awake is called when the script instance is loaded
    void Awake()
    {
        // Call base Awake to initialize base variables
        base.Awake();

        // Find the closest patrol area for this enemy
        PatrolArea area = FindClosestPatrolArea();

        // Get a reference to the enemy's sword collider (used for attack detection)
        Collider sword = GetComponentInChildren<AffectPlayer>().swordCollider;

        // Initialize all states in the dictionary with this enemy as context
        States[EnemyState.Idle] = new IdleStateFSM(EnemyState.Idle, this);
        States[EnemyState.Patrol] = new PatrolStateFSM(EnemyState.Patrol, this, area);
        States[EnemyState.Chase] = new ChaseStateFSM(EnemyState.Chase, this);
        States[EnemyState.Attack] = new AttackStateFSM(EnemyState.Attack, this, sword);
        States[EnemyState.Hit] = new HitStateFSM(EnemyState.Hit, this);
        States[EnemyState.Dead] = new DeadStateFSM(EnemyState.Dead, this);
        States[EnemyState.Block] = new BlockStateFSM(EnemyState.Block, this);
        States[EnemyState.BackDodge] = new BackDodgeStateFSM(EnemyState.BackDodge, this);
        States[EnemyState.BlockHit] = new BlockHitStateFSM(EnemyState.BlockHit, this);

        // Set the initial state to Patrol
        CurrentState = States[EnemyState.Patrol];
    }
    #endregion

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
