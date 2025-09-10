using UnityEngine;

public class AffectPlayer : MonoBehaviour
{
    [Header("References")]
    private CombatController CombatController;

    [Header("Damage Value")]
    [SerializeField] private int damage;

    [Header("Collider")]
    private Collider swordCollider;

    private void Start()
    {
        swordCollider = GetComponent<Collider>();
        //CombatController = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<CombatController>();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Hit Player");
            if(CombatController == null)
            {
                CombatController = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<CombatController>();
            }
            swordCollider.enabled = false;
            CombatController.TakeDamage(damage);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            swordCollider.enabled = true;
        }
    }
}
