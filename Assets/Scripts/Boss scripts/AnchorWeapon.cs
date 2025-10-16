using UnityEngine;

public class AnchorWeapon : MonoBehaviour
{
    public int AnchorDamage = 20;
    private Collider col;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        col = this.GetComponent<Collider>();
        EnableCollider(false);
    }

    public void EnableCollider(bool boolean)
    {
        col.enabled = boolean;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponentInChildren<CombatController>().TakeDamage(20);
            EnableCollider(false);
        }
    }
}
