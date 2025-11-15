using UnityEngine;

public class ChargeAttackDMG : MonoBehaviour
{
    CombatController player;
    public float chargeDamage = 30f;
    private Collider col;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        col = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (player == null)
            {
                player = other.GetComponentInChildren<CombatController>();
            }
            player.TDBB_With_Knockback((int)chargeDamage, this.transform);
            col.enabled = false;
        }
    }
}
