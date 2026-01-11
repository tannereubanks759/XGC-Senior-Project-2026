using UnityEngine;

public class DamageRef : MonoBehaviour
{
    private PirateBossAI anchorBoss;
    private MagmaBossAI magmaBoss;
    private GhostBossAI ghostBoss;
    private SkeletonSwordEnemy swordEnemy;

    public GameObject GoldBagPrefab;
    public int OverrideGoldAmount = 0;

    private bool hasSpawnedGold = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anchorBoss = GetComponent<PirateBossAI>();
        magmaBoss = GetComponent<MagmaBossAI>();
        ghostBoss = GetComponent<GhostBossAI>();
        swordEnemy = GetComponent<SkeletonSwordEnemy>();
        hasSpawnedGold = false;
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
        if (ghostBoss)
        {
            ghostBoss.TakeDamage((int)damage);
        }
        if (swordEnemy)
        {
            swordEnemy.ApplyDamage(damage, true);
        }
    }

    void SpawnGoldIfDead(int damage)
    {
        if (GoldBagPrefab == null && hasSpawnedGold) return;

        if (magmaBoss)
        {
            if (magmaBoss.currentHealth <= damage ) //spawn gold
            {
                SpawnGold();

            }
        }
        if (anchorBoss)
        {
            if (anchorBoss.currentHealth <= damage) //spawn gold
            {
                SpawnGold();

            }
        }
        if (ghostBoss)
        {
            if(ghostBoss.currentHealth <= damage)
            {
                SpawnGold();
            }
        }
    }

    void SpawnGold()
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
