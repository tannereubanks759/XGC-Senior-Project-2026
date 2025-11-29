using UnityEngine;

public class DamageRef : MonoBehaviour
{
    private PirateBossAI anchorBoss;
    private MagmaBossAI magmaBoss;

    public GameObject GoldBagPrefab;
    public int OverrideGoldAmount = 0; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anchorBoss = GetComponent<PirateBossAI>();
        magmaBoss = GetComponent<MagmaBossAI>();
    }

    public void TakeDamage(float damage)
    {
        SpawnGoldIfDead((int)damage);

        if (anchorBoss)
        {
            
            anchorBoss.TakeDamage((int) damage);
            
        }
        if (magmaBoss)
        {
            
            magmaBoss.TakeDamage((int)damage);
        }
    }

    void SpawnGoldIfDead(int damage)
    {
        if (magmaBoss)
        {
            if (magmaBoss.currentHealth <= damage && GoldBagPrefab != null) //spawn gold
            {
                if (OverrideGoldAmount > 0)
                {
                    GoldBag bag = Instantiate(GoldBagPrefab, transform.position, Quaternion.identity).GetComponent<GoldBag>();
                    bag.AmountOfGold = OverrideGoldAmount;
                }
                else
                {
                    Instantiate(GoldBagPrefab, transform.position, Quaternion.identity);
                }

            }
        }
        if (anchorBoss)
        {
            if (anchorBoss.currentHealth <= damage && GoldBagPrefab != null) //spawn gold
            {
                if (OverrideGoldAmount > 0)
                {
                    GoldBag bag = Instantiate(GoldBagPrefab, transform.position, Quaternion.identity).GetComponent<GoldBag>();
                    bag.AmountOfGold = OverrideGoldAmount;
                }
                else
                {
                    Instantiate(GoldBagPrefab, transform.position, Quaternion.identity);
                }

            }
        }
    }
}
