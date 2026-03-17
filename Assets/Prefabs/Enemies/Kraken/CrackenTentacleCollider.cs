using Unity.VisualScripting;
using UnityEngine;

public class CrackenTentacleCollider : MonoBehaviour
{
    public KrakenTentacle tentacle;

    private float nextHit = 0f;


    public float health = 50f;

    private KrakenManager km;
    public bool isCursed;
    public float curseDamageMult;

    private void Start()
    {
        km = GameObject.FindAnyObjectByType<KrakenManager>();
    }
    public void TakeDamage(float damage)
    {
        Debug.Log("Damage given to kracken tentacle");
        float finalDamage = isCursed ? damage * curseDamageMult : damage;

        if (health > finalDamage)
        {
            health -= finalDamage;
            km.TakeDamage(finalDamage);
        }
        else
        {
            km.TakeDamage(finalDamage);
            Die();
        }
    }
    private void Die()
    {
        Debug.Log("Kracken is dead");
        tentacle.Death();
        
    }
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.tag == "Player" && Time.time > nextHit && tentacle.isDropping)
        {
            collision.gameObject.GetComponentInChildren<CombatController>().TakeDamageByBoss(30);
            nextHit = Time.time + 5f;
        }

        this.GetComponent<Collider>().enabled = false;

        if (tentacle.isDropping == true)
        {
            tentacle.GoUp();
        }
        tentacle.isDropping = false;
        
        
            
        
        
        
    }
}
