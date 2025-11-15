using UnityEngine;

public class BossHand : MonoBehaviour
{
    CombatController player;
    public float handDamage;
    private Collider col;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        col = GetComponent<Collider>();
    }

    public void EnableCollider(bool enabled)
    {
        col.enabled = enabled;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if(player == null)
            {
                player = other.GetComponentInChildren<CombatController>();
            }
            player.TDBB_With_Knockback((int)handDamage, this.transform);
            col.enabled = false;
        }
    }
}
