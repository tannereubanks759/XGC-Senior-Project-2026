using UnityEngine;
using Unity.VisualScripting;
using RayFire;

public class BasicDeadState : BaseState<EnemyState>
{
    private BaseEnemyAI _enemy;
    private RayfireRigid rf;
    private GameObject sword;
    public BasicDeadState(EnemyState key, BaseEnemyAI enemy, RayfireRigid rayf, GameObject sw) : base(key)
    {
        _enemy = enemy;
        rf = rayf;
        sword = sw;
    }

    public override void EnterState()
    {
        //Debug.Log("Entered Dead State");

        //var deathIndex = Random.Range(0, 2);

        //_enemy.Animator.SetInteger("DeadIndex", deathIndex);

        //_enemy.SetResetTriggers("Dead");

        //_enemy.Animator.SetFloat("Speed", Mathf.Floor(0));
        _enemy.Animator.enabled = false;

        _enemy.Agent.ResetPath();

        _enemy.Agent.enabled = false;

        //_enemy.GetComponent<Collider>().enabled = false;

        //_enemy.combatQueue.RemoveAttackingEnemy(_enemy.GetComponent<BasicSkeleton>());
        GameObject parent = sword.GetComponentInParent<BaseEnemyAI>().gameObject;
        sword.GetComponent<Rigidbody>().isKinematic = false;
        Collider col = sword.GetComponent<Collider>();
        col.enabled = true;
        col.isTrigger = false;
        sword.GetComponent<AffectPlayer>().enabled = false;
        sword.layer = 12;
        sword.transform.parent = null;
        
        // Needs work
        //_enemy.AddComponent<DeathCull>();
        rf.Demolish();
        parent.gameObject.SetActive(false);
    }

    public override void ExitState()
    {

    }

    public override EnemyState GetNextState()
    {
        return StateKey;
    }

    public override void UpdateState()
    {

    }
}
