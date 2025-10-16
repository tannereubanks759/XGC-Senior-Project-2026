using UnityEngine;
using System.Collections.Generic;

public class BasicSkeleton : BaseEnemyAI
{
    [Header("Basic Skeleton Ref")]
    [Tooltip("Refs to the skeletons armor")]
    public List<GameObject> armor;
    public List<List<GameObject>> armorLOD;
    [Tooltip("Ref to the shield")]
    public GameObject shield;
    public bool isShieldedEnemy = false;

    private bool setNextTwo = false;

    private void Awake()
    {
        base.Awake();

        PatrolArea area = FindClosestPatrolArea();

        States[EnemyState.Idle] = new BasicIdleState(EnemyState.Idle, this);
        States[EnemyState.Patrol] = new BasicPatrolState(EnemyState.Patrol, this, area);
        States[EnemyState.Chase] = new BasicChaseState(EnemyState.Chase, this, this);
        States[EnemyState.Attack] = new BasicAttackState(EnemyState.Attack, this);
        States[EnemyState.Block] = new BasicBlockState(EnemyState.Block, this);
        States[EnemyState.Hit] = new BasicHitState(EnemyState.Hit, this);
        States[EnemyState.Dead] = new BasicDeadState(EnemyState.Dead, this);

        CurrentState = States[EnemyState.Idle];

        SetArmor();
    }

    void SetArmor()
    {
        var index = 2;

        foreach (var item in armor)
        {
            if (setNextTwo && index > 0)
            {
                item.SetActive(true);
                index--;
            }
            else
            {
                var roll = Random.value;
                if (roll <= .33)
                {
                    item.SetActive(true);
                    setNextTwo = true;
                    index = 2;
                }
            }
        }
        /*
        var rollTwo = Random.value;
        Debug.Log(rollTwo);
        if (rollTwo <= .25)
        {
            shield.SetActive(true);
            isShieldedEnemy = true;
        }
        */
    }
}
