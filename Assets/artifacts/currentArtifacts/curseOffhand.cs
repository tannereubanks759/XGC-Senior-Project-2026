using Unity.VisualScripting;
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
    [Header("Curse Timeout")]
    public float curseDuration = 8f;
    private float curseExpireTime = 0f;
    public float curseReflectPercentL = .25f;
    public upgradeTracker upgradeTracker;
    [Header("Curse Preview")]
    public GameObject cursePreviewVfxPrefab;
    private GameObject previewInstance;
    private DamageRef previewTarget;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    public void slowUpgradae()
    {
        slowUpgrade = true;
        upgradeTracker.curseSlow = true;
    }

    public void reflectionUpgradae()
    {
        reflectionUpgrade = true;
        upgradeTracker.curseReflect = true;
    }

    private void checkUpgrade(BaseEnemyAI baseAI)
    {
        
        /*if(reflectionUpgrade)
        {

        }*/
    }

    void Update()
    {
        HandleCursePreview();
        if (cursedTarget == null)
        {
            if (spawnedCurseVfx != null)
            {
                Destroy(spawnedCurseVfx);
                spawnedCurseVfx = null;
            }
            EnsureFlameState();
        }
        if (cursedTarget != null && IsDead(cursedTarget))
        {
            ClearCurse();
            return;
        }
        if (cursedTarget != null && Time.time >= curseExpireTime)
        {
            ClearCurse();
            EnsureFlameState();
            return;
        }
        if (!canCurse)
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
                ClearPreview();
                curseExpireTime = Time.time + curseDuration;
                enemyScript.damageMult = damageMult;
                Vector3 offset = new Vector3(0f, 1.3f, 0f);
                if (enemyScript.curseVfxPrefab != null)
                {
                    if (activeCurseVfx != null) Destroy(activeCurseVfx);
                    spawnedCurseVfx = Instantiate(enemyScript.curseVfxPrefab, enemy.transform.position + offset, Quaternion.identity, enemy.transform);
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
                        if (activeCurseVfx != null)
                        {
                            Vector3 offset = new Vector3(0f, 0f, 0f);
                            if (spawnedCurseVfx != null) Destroy(spawnedCurseVfx);
                            Transform follow = getChest(pirateboss.transform);
                            spawnedCurseVfx = Instantiate(activeCurseVfx, follow.position, Quaternion.identity, follow);
                            spawnedCurseVfx.transform.localPosition = offset;
                            /*spawnedCurseVfx = Instantiate(activeCurseVfx, pirateboss.transform.position + offset, Quaternion.identity, pirateboss.transform);
                            spawnedCurseVfx.transform.localPosition = offset;*/
                        }
                    }
                    pirateboss.curseBoss(slowUpgrade, reflectionUpgrade);
                    cursedTarget = hitRef;
                    ClearPreview();
                    curseExpireTime = Time.time + curseDuration;
                    if (cursedFlame != null) cursedFlame.SetActive(false);
                    return;
                }
                // Magma boss
                if (magmaBoss != null)
                {
                    if (!magmaBoss.isCursed)
                    {
                        if (activeCurseVfx != null)
                        {
                            Vector3 offset = new Vector3(0f, 0f, 0f);
                            if (spawnedCurseVfx != null) Destroy(spawnedCurseVfx);
                            Transform follow = getChest(magmaBoss.transform);
                            spawnedCurseVfx = Instantiate(activeCurseVfx, follow.position, Quaternion.identity, follow);
                            spawnedCurseVfx.transform.localPosition = offset;
                        }
                    }
                    magmaBoss.CurseBoss(slowUpgrade, reflectionUpgrade);
                    cursedTarget = hitRef;
                    ClearPreview();
                    curseExpireTime = Time.time + curseDuration;
                    if (cursedFlame != null) cursedFlame.SetActive(false);
                    return;
                }
                // Ghost boss logic
                if (ghostBoss != null)
                {
                    if (!ghostBoss.isCursed)
                    {
                        if (activeCurseVfx != null)
                        {
                            Vector3 offset = new Vector3(0f, 0f, 0f);
                            if (spawnedCurseVfx != null) Destroy(spawnedCurseVfx);
                            Transform follow = getChest(ghostBoss.transform);
                            spawnedCurseVfx = Instantiate(activeCurseVfx, follow.position, Quaternion.identity, follow);
                            spawnedCurseVfx.transform.localPosition = offset;
                        }
                    }
                    ghostBoss.CurseBoss(slowUpgrade, reflectionUpgrade);
                    cursedTarget = hitRef;
                    ClearPreview();
                    curseExpireTime = Time.time + curseDuration;
                    if (cursedFlame != null) cursedFlame.SetActive(false);
                    return;
                }
                if (swordEnemy != null)
                {
                    cursedTarget = hitRef;
                    ClearPreview();
                    curseExpireTime = Time.time + curseDuration;
                    swordEnemy.isCursed = true;
                    swordEnemy.curseDamageMult = damageMult;
                    swordEnemy.curseSpeedMult = slowUpgrade ? slowSpeedMultiplier : 1f;
                    swordEnemy.curseReflectEnabled = reflectionUpgrade;
                    
                    swordEnemy.curseReflectPercent = curseReflectPercentL;
                    if (activeCurseVfx != null)
                    {
                        Vector3 offset = new Vector3(0f, 0f, 0f);
                        if (spawnedCurseVfx != null)
                        {
                            Destroy(spawnedCurseVfx);
                            spawnedCurseVfx = null;
                        }
                        Transform follow = getChest(swordEnemy.transform);
                        spawnedCurseVfx = Instantiate(activeCurseVfx, follow.position, Quaternion.identity, follow);
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
    private void ClearCurse()
    {
        if (cursedTarget == null) return;
        var pirate = cursedTarget.GetComponentInParent<PirateBossAI>();
        if (pirate != null)
        {
            pirate.RemoveCurse();
        }
        var magma = cursedTarget.GetComponentInParent<MagmaBossAI>();
        if (magma != null)
        {
            magma.RemoveCurse();
        }
        var ghost = cursedTarget.GetComponentInParent<GhostBossAI>();
        if (ghost != null)
        {
            ghost.RemoveCurse();
        }
        var swordEnemy = cursedTarget.GetComponentInParent<SkeletonSwordEnemy>();
        if (swordEnemy != null)
        {
            swordEnemy.isCursed = false;
            swordEnemy.curseDamageMult = 1;
            swordEnemy.curseSpeedMult = 1f;
            swordEnemy.curseReflectEnabled = false;
        }
        if (spawnedCurseVfx != null)
        {
            Destroy(spawnedCurseVfx);
            spawnedCurseVfx = null;
        }
        cursedTarget = null;
        EnsureFlameState();
    }
    private Transform getChest(Transform enemyRoot)
    {
        Animator anim = enemyRoot.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            Transform t = anim.GetBoneTransform(HumanBodyBones.Chest);
            if (t != null) return t;
            return anim.transform;
        }
        return enemyRoot;
    }
    private void HandleCursePreview()
    {
        if (cursePreviewVfxPrefab == null || Camera.main == null)
        {
            ClearPreview();
            return;
        }

        if (!canCurse || cursedTarget != null || AnyBossCursed())
        {
            ClearPreview();
            return;
        }

        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, curseRange, enemyMask, QueryTriggerInteraction.Ignore))
        {
            ClearPreview();
            return;
        }

        DamageRef hitRef = hit.collider.GetComponentInParent<DamageRef>();
        if (hitRef == null || IsAlreadyCursed(hitRef))
        {
            ClearPreview();
            return;
        }

        Transform enemyRoot = GetEnemyRootFromHitRef(hitRef);
        if (enemyRoot == null)
        {
            ClearPreview();
            return;
        }

        if (previewTarget != hitRef)
        {
            ClearPreview();
            previewTarget = hitRef;
            Transform follow = getChest(enemyRoot);
            previewInstance = Instantiate(cursePreviewVfxPrefab, follow.position, Quaternion.identity, follow);
            previewInstance.transform.localPosition = Vector3.zero;
            previewInstance.transform.localRotation = Quaternion.identity;
            //previewInstance.transform.localScale = Vector3.one;
            Debug.Log($"[CursePreview] Hit: {hit.collider.name} dist={hit.distance:F2} | enemyRoot={enemyRoot.name} | follow={follow.name}");
        }
    }


    private void ClearPreview()
    {
        previewTarget = null;

        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
        }
    }
    private bool AnyBossCursed()
    {
        return FindAnyObjectByType<PirateBossAI>()?.isCursed == true || FindAnyObjectByType<MagmaBossAI>()?.isCursed == true || FindAnyObjectByType<GhostBossAI>()?.isCursed == true;
    }
    private bool IsAlreadyCursed(DamageRef target)
    {
        if (target == null) return true;
        var pirate = target.GetComponentInParent<PirateBossAI>();
        if (pirate != null) return pirate.isCursed;
        var magma = target.GetComponentInParent<MagmaBossAI>();
        if (magma != null) return magma.isCursed;
        var ghost = target.GetComponentInParent<GhostBossAI>();
        if (ghost != null) return ghost.isCursed;
        var sword = target.GetComponentInParent<SkeletonSwordEnemy>();
        if (sword != null) return sword.isCursed;
        return false;
    }
    private Transform GetEnemyRootFromHitRef(DamageRef hitRef)
    {
        if (hitRef == null) return null;
        var baseEnemy = hitRef.GetComponentInParent<BaseEnemyAI>();
        if (baseEnemy != null) return baseEnemy.transform;
        var pirate = hitRef.GetComponentInParent<PirateBossAI>();
        if (pirate != null) return pirate.transform;
        var magma = hitRef.GetComponentInParent<MagmaBossAI>();
        if (magma != null) return magma.transform;
        var ghost = hitRef.GetComponentInParent<GhostBossAI>();
        if (ghost != null) return ghost.transform;
        var sword = hitRef.GetComponentInParent<SkeletonSwordEnemy>();
        if (sword != null) return sword.transform;
        return hitRef.transform.root;
    }
    private void OnDisable()
    {
        ClearPreview();
    }
}
