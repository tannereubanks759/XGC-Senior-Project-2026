using Unity.VisualScripting;
using UnityEngine;

public class CrackenTentacleCollider : MonoBehaviour
{
    public KrakenTentacle tentacle;

    private float nextHit = 0f;


    public float health = 50f;
    public void TakeDamage(float damage)
    {
        Debug.Log("Damage given to kracken tentacle");
        if(health >= damage)
        {
            health -= damage;
        }
        else
        {
            Die();
        }
    }
    private void Die()
    {
        Debug.Log("Kracken is dead");
        tentacle.Death();
    }
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Player" && Time.time > nextHit && tentacle.isDropping)
        {
            collision.gameObject.GetComponentInChildren<CombatController>().TakeDamageByBoss(30);
            nextHit = Time.time + 5f;
        }
        if (tentacle.isDropping)
        {
            tentacle.isDropping = false;
            tentacle.GoUp();
        }
    }
}
