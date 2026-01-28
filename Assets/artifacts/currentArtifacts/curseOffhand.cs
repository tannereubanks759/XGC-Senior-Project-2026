using UnityEngine;
using UnityEngine.AI;

public class curseOffhand : MonoBehaviour
{
    //public bool isActive;
    // public BaseEnemyAI cursedEnemy;
    public DamageRef cursedTarget;
    private int curseRange = 10;
    public LayerMask enemyMask;
    public int damageMult = 2;
    public bool slowUpgrade = false;
    public bool reflectionUpgrade = false;
    private float slowedSpeed;
    public GameObject cursedFlame;
    public GameObject activeCurseVfx;
    private GameObject spawnedCurseVfx;
    public bool canCurse = false;
    [Range(0.5f, 1f)] public float slowSpeedMultiplier = 0.55f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    public void slowUpgradae()
    {
        slowUpgrade = true;
    }

    public void reflectionUpgradae()
    {
        reflectionUpgrade = true;
    }

    private void checkUpgrade(BaseEnemyAI baseAI)
    {
        
        /*if(reflectionUpgrade)
        {

        }*/
    }

    void Update()
    {
        if (cursedTarget != null && IsDead(cursedTarget))
        {
            cursedTarget = null;
            if (spawnedCurseVfx != null)
            {
                Destroy(spawnedCurseVfx);
                spawnedCurseVfx = null;
            }
            if (activeCurseVfx != null)
            {
                Destroy(activeCurseVfx);
                activeCurseVfx = null;
            }
            if (cursedFlame != null)
            {
                cursedFlame.SetActive(true);
            }
        }

        if (!canCurse)
        {
            EnsureFlameState();
            return;
        }

        if (!Input.GetKeyDown(KeyCode.F))
        {
            EnsureFlameState();
            return;
        }

        // only run when f is pressed
        if (!Input.GetKeyDown(KeyCode.F))
        {
            EnsureFlameState();
            return;
        }

        if (cursedTarget != null || FindAnyObjectByType<PirateBossAI>()?.isCursed == true || FindAnyObjectByType<MagmaBossAI>()?.isCursed == true || FindAnyObjectByType<GhostBossAI>()?.isCursed == true)
        {
            return;
        }

        Ray curseRay = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (Physics.Raycast(curseRay, out RaycastHit hit, curseRange, enemyMask))
        {
            DamageRef hitRef = hit.collider.GetComponentInParent<DamageRef>();
            if (hitRef == null) return;

            BaseEnemyAI enemy = hitRef.GetComponentInParent<BaseEnemyAI>();
            if (enemy != null)
            {
                Debug.Log("Applied curse");
                var enemyScript = enemy.GetComponent<BaseEnemyAI>();
                cursedTarget = hitRef;
                enemyScript.damageMult = damageMult;
                Vector3 offset = new Vector3(0f, 1.3f, 0f);
                if (enemyScript.curseVfxPrefab != null)
                {
                    if (activeCurseVfx != null) Destroy(activeCurseVfx);
                    activeCurseVfx = Instantiate(enemyScript.curseVfxPrefab, enemy.transform.position + offset, Quaternion.identity, enemy.transform);
                    activeCurseVfx.transform.localPosition = offset;
                }
                checkUpgrade(enemyScript);

                if (cursedFlame != null) cursedFlame.SetActive(false);
                return;
            }
            else
            {
                PirateBossAI pirateboss = hitRef.GetComponentInParent<PirateBossAI>();
                MagmaBossAI magmaBoss = hitRef.GetComponentInParent<MagmaBossAI>();
                GhostBossAI ghostBoss = hitRef.GetComponentInParent<GhostBossAI>();
                SkeletonSwordEnemy swordEnemy = hitRef.GetComponentInParent<SkeletonSwordEnemy>();

                // Pirate boss
                if (pirateboss != null)
                {
                    if (!pirateboss.isCursed)
                    {
                        Vector3 offset = new Vector3(0f, 2f, 0f);
                        if (pirateboss.cursedVfxPrefab != null)
                        {
                            if (activeCurseVfx != null) Destroy(activeCurseVfx);
                            activeCurseVfx = Instantiate(pirateboss.cursedVfxPrefab, pirateboss.transform.position + offset, Quaternion.identity, pirateboss.transform);
                            activeCurseVfx.transform.localPosition = offset;
                        }
                    }

                    pirateboss.curseBoss(slowUpgrade, reflectionUpgrade);

                    cursedTarget = hitRef;
                    if (cursedFlame != null) cursedFlame.SetActive(false);
                    return;
                }

                // Magma boss
                if (magmaBoss != null)
                {
                    if (!magmaBoss.isCursed)
                    {
                        Vector3 offset = new Vector3(0f, 1.6f, 0f);
                        if (magmaBoss.cursedVfxPrefab != null)
                        {
                            if (activeCurseVfx != null) Destroy(activeCurseVfx);
                            activeCurseVfx = Instantiate(magmaBoss.cursedVfxPrefab, magmaBoss.transform.position + offset, Quaternion.identity, magmaBoss.transform);
                            activeCurseVfx.transform.localPosition = offset;
                        }
                    }

                    magmaBoss.CurseBoss(slowUpgrade, reflectionUpgrade);

                    cursedTarget = hitRef;
                    if (cursedFlame != null) cursedFlame.SetActive(false);
                    return;
                }

                // Ghost boss logic
                if (ghostBoss != null)
                {
                    if (!ghostBoss.isCursed)
                    {
                        Vector3 offset = new Vector3(0f, 1.6f, 0f);
                        if (ghostBoss.cursedVfxPrefab != null)
                        {
                            if (activeCurseVfx != null) Destroy(activeCurseVfx);
                            activeCurseVfx = Instantiate(ghostBoss.cursedVfxPrefab, ghostBoss.transform.position + offset, Quaternion.identity, ghostBoss.transform);
                            activeCurseVfx.transform.localPosition = offset;
                        }
                    }

                    ghostBoss.CurseBoss(slowUpgrade, reflectionUpgrade);

                    cursedTarget = hitRef;
                    if (cursedFlame != null) cursedFlame.SetActive(false);
                    return;
                }
                if (swordEnemy != null)
                {
                    cursedTarget = hitRef;
                    swordEnemy.isCursed = true;
                    swordEnemy.curseDamageMult = damageMult;
                    swordEnemy.curseSpeedMult = slowUpgrade ? slowSpeedMultiplier : 1f;
                    swordEnemy.curseReflectEnabled = reflectionUpgrade;
                    swordEnemy.curseReflectPercent = 0.25f;
                    if (activeCurseVfx != null)
                    {
                        Vector3 offset = new Vector3(0f, 1.3f, 0f);
                        if (spawnedCurseVfx != null)
                        {
                            Destroy(spawnedCurseVfx);
                            spawnedCurseVfx = null;
                        }
                        spawnedCurseVfx = Instantiate(activeCurseVfx, swordEnemy.transform.position + offset, Quaternion.identity, swordEnemy.transform);
                        spawnedCurseVfx.transform.localPosition = offset;
                    }
                    if (cursedFlame != null)
                    {
                        cursedFlame.SetActive(false);
                    }
                    return;
                }

            }
        }

        EnsureFlameState();
    }

    private bool IsDead(DamageRef target)
    {
        if (target == null) return true;

        var magma = target.GetComponentInParent<MagmaBossAI>();
        if (magma != null) return magma.currentHealth <= 0;
        var pirate = target.GetComponentInParent<PirateBossAI>();
        if (pirate != null) return pirate.currentHealth <= 0;
        var ghost = target.GetComponentInParent<GhostBossAI>();
        if (ghost != null) return ghost.currentHealth <= 0;
        var sword = target.GetComponentInParent<SkeletonSwordEnemy>();
        if (sword != null) return sword.GetHealth() <= 0;
        var baseEnemy = target.GetComponentInParent<BaseEnemyAI>();
        if (baseEnemy != null) return baseEnemy.currentHealth <= 0;

        return false;
    }
   
    private void EnsureFlameState()
    {
        if (cursedFlame == null) return;

        bool anyBossCursed = FindAnyObjectByType<PirateBossAI>()?.isCursed == true || FindAnyObjectByType<MagmaBossAI>()?.isCursed == true || FindAnyObjectByType<GhostBossAI>()?.isCursed == true;

        if (cursedTarget == null && !anyBossCursed)
        {
            if (!cursedFlame.activeSelf) cursedFlame.SetActive(true);
        }
        else
        {
            if (cursedFlame.activeSelf) cursedFlame.SetActive(false);
        }
    }
}
