using UnityEngine;

public class AffectPlayer : MonoBehaviour
{
    [Header("References")]
    private CombatController combatController;
    private BaseEnemyAI enemyAI;

    [Header("Damage Value")]
    [SerializeField] private int damage = 10;

    [Header("Collider")]
    public Collider swordCollider;

    private void Awake()
    {
        if (swordCollider != null) swordCollider.enabled = false;

        // Get the parent enemy AI
        enemyAI = GetComponentInParent<BaseEnemyAI>();
    }

    private void Start()
    {
        combatController = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<CombatController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        swordCollider.enabled = false;

        Vector3 hitDir = (other.transform.position - transform.position).normalized;

        if (enemyAI != null)
        {
            combatController.TakeDamage(damage, hitDir, enemyAI.eliteType);
        }
        else
        {
            combatController.TakeDamage(damage, hitDir);
        }
    }

}
