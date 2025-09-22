using UnityEngine;

public class AffectPlayer : MonoBehaviour
{
    [Header("References")]
    private CombatController CombatController;

    [Header("Damage Value")]
    [SerializeField] private int damage;

    [Header("Collider")]
    public Collider swordCollider;

    private void Awake()
    {
        swordCollider.enabled = false;
        Debug.Log("Has Sword Collider: " + swordCollider);
        //CombatController = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<CombatController>();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            swordCollider.enabled = false;
            Debug.Log("Hit Player");
            if(CombatController == null)
            {
                CombatController = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<CombatController>();
            }
            CombatController.TakeDamage(damage);
        }
    }

}
