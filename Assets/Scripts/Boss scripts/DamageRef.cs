using UnityEngine;

public class DamageRef : MonoBehaviour
{
    private PirateBossAI anchorBoss;
    private MagmaBossAI magmaBoss;
    private GhostBossAI ghostBoss;
    private SkeletonSwordEnemy swordEnemy;
    private SkeletonGunEnemy gunEnemy;
    private CrackenTentacleCollider kraken;

    public GameObject SoulPrefab;
    public int OverrideSoulAmount = 0;

    private bool hasSpawnedSoul = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anchorBoss = GetComponent<PirateBossAI>();
        magmaBoss = GetComponent<MagmaBossAI>();
        ghostBoss = GetComponent<GhostBossAI>();
        swordEnemy = GetComponent<SkeletonSwordEnemy>();
        kraken = GetComponent<CrackenTentacleCollider>();
        gunEnemy = GetComponent<SkeletonGunEnemy>();
        hasSpawnedSoul = false;
    }

    public void TakeDamage(float damage)
    {
        SpawnSoulIfDead((int)damage);

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
        if (kraken)
        {
            kraken.TakeDamage(damage);
        }
        if (gunEnemy)
        {
            gunEnemy.ApplyDamage(damage);
        }
    }

    void SpawnSoulIfDead(int damage)
    {
        if (SoulPrefab == null || hasSpawnedSoul) return;

        if (magmaBoss)
        {
            if (magmaBoss.currentHealth <= damage ) //spawn gold
            {
                SpawnSoul();

            }
        }
        if (anchorBoss)
        {
            if (anchorBoss.currentHealth <= damage) //spawn gold
            {
                SpawnSoul();

            }
        }
        if (ghostBoss)
        {
            if(ghostBoss.currentHealth <= damage)
            {
                SpawnSoul();
            }
        }
        if (swordEnemy)
        {
            if (swordEnemy.GetHealth() <= damage)
            {
                SpawnSoul();
            }
        }
        if (gunEnemy)
        {
            if(gunEnemy.GetHealth() <= damage)
            {
                SpawnSoul();
            }
        }
    }

    void SpawnSoul()
    {
        if (OverrideSoulAmount > 0)
        {
            SoulScript s = Instantiate(SoulPrefab, transform.position + new Vector3(0, 1, 0), Quaternion.identity).GetComponent<SoulScript>();
            s.amountOfSouls = OverrideSoulAmount;
        }
        else
        {
            Instantiate(SoulPrefab, transform.position + new Vector3(0,1,0), Quaternion.identity);
        }
    }
}
