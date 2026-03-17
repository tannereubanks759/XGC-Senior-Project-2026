using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class curseOffhand : MonoBehaviour
{
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
    private bool curseActive = false;
    [Range(0.5f, 1f)] public float slowSpeedMultiplier = 0.55f;
    [Header("Curse Timeout")]
    public float curseDuration = 8f;
    private float curseExpireTime = 0f;
    public float curseReflectPercentL = .25f;
    public upgradeTracker upgradeTracker;
    [Header("Curse Selection")]
    public GameObject cursePreviewVfxPrefab;
    private GameObject previewInstance;
    private DamageRef previewTarget;
    public float curseCastRadius = 0.35f;
    [Header("Audio")]
    public AudioSource source;
    public AudioClip equipClip;
    [Range(0f, 1f)] public float equipVol = 0.8f;
    public AudioClip previewClip;
    [Range(0f, 1f)] public float previewVol = 0.5f;
    public AudioClip applyClip;
    [Range(0f, 1f)] public float applyVol = 0.9f;
    public AudioClip expireClip;
    [Range(0f, 1f)] public float expireVol = 0.7f;
    private bool played = false;

    void Start() { }

    private void PlaySound(AudioClip clip, float vol = 1f)
    {
        if (source != null && clip != null)
            source.PlayOneShot(clip, vol);
    }

    private void OnEnable()
    {
        ClearPreview();
        played = true;
        PlaySound(equipClip, equipVol);
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

    private void checkUpgrade(BaseEnemyAI baseAI) { }

    private void EndCurseEffectsOnly()
    {
        if (!curseActive) return;
        curseActive = false;
        PlaySound(expireClip, expireVol);
        if (spawnedCurseVfx != null)
        {
            Destroy(spawnedCurseVfx);
            spawnedCurseVfx = null;
        }
        EnsureFlameState();
    }

    void Update()
    {
        HandleCursePreview();

        if (curseActive && cursedTarget == null)
        {
            EndCurseEffectsOnly();
            return;
        }

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

        if (!Input.GetKeyDown(KeyCode.F))
        {
            EnsureFlameState();
            return;
        }

        if (cursedTarget != null ||
            FindAnyObjectByType<PirateBossAI>()?.isCursed == true ||
            FindAnyObjectByType<MagmaBossAI>()?.isCursed == true ||
            FindAnyObjectByType<GhostBossAI>()?.isCursed == true)
        {
            return;
        }

        Ray curseRay = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (Physics.SphereCast(curseRay, curseCastRadius, out RaycastHit hit, curseRange, enemyMask, QueryTriggerInteraction.Ignore))
        {
            DamageRef hitRef = hit.collider.GetComponentInParent<DamageRef>();
            if (hitRef == null) return;

            //old enemy
            BaseEnemyAI enemy = hitRef.GetComponentInParent<BaseEnemyAI>();
            if (enemy != null)
            {
                cursedTarget = hitRef;
                curseActive = true;
                PlaySound(applyClip, applyVol);
                ClearPreview();
                curseExpireTime = Time.time + curseDuration;
                enemy.damageMult = damageMult;
                Vector3 offset = new Vector3(0f, 1.3f, 0f);
                if (enemy.curseVfxPrefab != null)
                {
                    if (spawnedCurseVfx != null) Destroy(spawnedCurseVfx);
                    spawnedCurseVfx = Instantiate(enemy.curseVfxPrefab, enemy.transform.position + offset, Quaternion.identity, enemy.transform);
                    spawnedCurseVfx.transform.localPosition = offset;
                }
                checkUpgrade(enemy);
                if (cursedFlame != null) cursedFlame.SetActive(false);
                return;
            }

            PirateBossAI pirateboss = hitRef.GetComponentInParent<PirateBossAI>();
            MagmaBossAI magmaBoss = hitRef.GetComponentInParent<MagmaBossAI>();
            GhostBossAI ghostBoss = hitRef.GetComponentInParent<GhostBossAI>();
            CrackenTentacleCollider kraken = hitRef.GetComponentInParent<CrackenTentacleCollider>();
            SkeletonSwordEnemy swordEnemy = hitRef.GetComponentInParent<SkeletonSwordEnemy>();
            SkeletonGunEnemy gunEnemy = hitRef.GetComponentInParent<SkeletonGunEnemy>();

            // Kraken tentacle
            if (kraken != null)
            {
                if (!kraken.isCursed)
                {
                    kraken.isCursed = true;
                    kraken.curseDamageMult = damageMult;

                    if (activeCurseVfx != null)
                    {
                        if (spawnedCurseVfx != null) Destroy(spawnedCurseVfx);
                        spawnedCurseVfx = Instantiate(activeCurseVfx, kraken.transform.position, Quaternion.identity, kraken.transform);
                        spawnedCurseVfx.transform.localPosition = Vector3.zero;
                    }

                    cursedTarget = hitRef;
                    curseActive = true;
                    Debug.Log("Kraken tentacle cursed!");
                    PlaySound(applyClip, applyVol);
                    ClearPreview();
                    curseExpireTime = Time.time + curseDuration;
                    if (cursedFlame != null) cursedFlame.SetActive(false);
                }
                return;
            }

            // Pirate boss
            if (pirateboss != null)
            {
                if (!pirateboss.isCursed)
                {
                    if (activeCurseVfx != null)
                    {
                        if (spawnedCurseVfx != null) Destroy(spawnedCurseVfx);
                        Transform follow = getChest(pirateboss.transform);
                        spawnedCurseVfx = Instantiate(activeCurseVfx, follow.position, Quaternion.identity, follow);
                        spawnedCurseVfx.transform.localPosition = Vector3.zero;
                    }
                }
                pirateboss.curseBoss(slowUpgrade, reflectionUpgrade);
                cursedTarget = hitRef;
                curseActive = true;
                PlaySound(applyClip, applyVol);
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
                        if (spawnedCurseVfx != null) Destroy(spawnedCurseVfx);
                        Transform follow = getChest(magmaBoss.transform);
                        spawnedCurseVfx = Instantiate(activeCurseVfx, follow.position, Quaternion.identity, follow);
                        spawnedCurseVfx.transform.localPosition = Vector3.zero;
                    }
                }
                magmaBoss.CurseBoss(slowUpgrade, reflectionUpgrade);
                cursedTarget = hitRef;
                curseActive = true;
                PlaySound(applyClip, applyVol);
                ClearPreview();
                curseExpireTime = Time.time + curseDuration;
                if (cursedFlame != null) cursedFlame.SetActive(false);
                return;
            }

            // Ghost boss
            if (ghostBoss != null)
            {
                if (!ghostBoss.isCursed)
                {
                    if (activeCurseVfx != null)
                    {
                        if (spawnedCurseVfx != null) Destroy(spawnedCurseVfx);
                        Transform follow = getChest(ghostBoss.transform);
                        spawnedCurseVfx = Instantiate(activeCurseVfx, follow.position, Quaternion.identity, follow);
                        spawnedCurseVfx.transform.localPosition = Vector3.zero;
                    }
                }
                ghostBoss.CurseBoss(slowUpgrade, reflectionUpgrade);
                cursedTarget = hitRef;
                curseActive = true;
                PlaySound(applyClip, applyVol);
                ClearPreview();
                curseExpireTime = Time.time + curseDuration;
                if (cursedFlame != null) cursedFlame.SetActive(false);
                return;
            }

            // Skeleton sword enemy
            if (swordEnemy != null)
            {
                cursedTarget = hitRef;
                curseActive = true;
                PlaySound(applyClip, applyVol);
                ClearPreview();
                curseExpireTime = Time.time + curseDuration;
                swordEnemy.isCursed = true;
                swordEnemy.curseDamageMult = damageMult;
                swordEnemy.curseSpeedMult = slowUpgrade ? slowSpeedMultiplier : 1f;
                swordEnemy.curseReflectEnabled = reflectionUpgrade;
                swordEnemy.curseReflectPercent = curseReflectPercentL;
                if (activeCurseVfx != null)
                {
                    if (spawnedCurseVfx != null) { Destroy(spawnedCurseVfx); spawnedCurseVfx = null; }
                    Transform follow = getChest(swordEnemy.transform);
                    spawnedCurseVfx = Instantiate(activeCurseVfx, follow.position, Quaternion.identity, follow);
                    spawnedCurseVfx.transform.localPosition = Vector3.zero;
                }
                if (cursedFlame != null) cursedFlame.SetActive(false);
                return;
            }

            // Skeleton gun enemy
            if (gunEnemy != null)
            {
                cursedTarget = hitRef;
                curseActive = true;
                PlaySound(applyClip, applyVol);
                ClearPreview();
                curseExpireTime = Time.time + curseDuration;
                gunEnemy.isCursed = true;
                gunEnemy.curseDamageMult = damageMult;
                gunEnemy.curseSpeedMult = slowUpgrade ? slowSpeedMultiplier : 1f;
                if (activeCurseVfx != null)
                {
                    if (spawnedCurseVfx != null) { Destroy(spawnedCurseVfx); spawnedCurseVfx = null; }
                    Transform follow = getChest(gunEnemy.transform);
                    spawnedCurseVfx = Instantiate(activeCurseVfx, follow.position, Quaternion.identity, follow);
                    spawnedCurseVfx.transform.localPosition = Vector3.zero;
                }
                if (cursedFlame != null) cursedFlame.SetActive(false);
                return;
            }
        }

        EnsureFlameState();
    }

    private bool IsDead(DamageRef target)
    {
        if (target == null) return true;

        var kraken = target.GetComponentInParent<CrackenTentacleCollider>();
        if (kraken != null) return kraken.health <= 0f;

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
        bool anyBossCursed = FindAnyObjectByType<PirateBossAI>()?.isCursed == true ||
                             FindAnyObjectByType<MagmaBossAI>()?.isCursed == true ||
                             FindAnyObjectByType<GhostBossAI>()?.isCursed == true;

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
        if (!curseActive) return;
        curseActive = false;
        PlaySound(expireClip, expireVol);

        if (cursedTarget != null)
        {
            var kraken = cursedTarget.GetComponentInParent<CrackenTentacleCollider>();
            if (kraken != null)
            {
                kraken.isCursed = false;
                kraken.curseDamageMult = 1;
            }

            var pirate = cursedTarget.GetComponentInParent<PirateBossAI>();
            if (pirate != null) pirate.RemoveCurse();

            var magma = cursedTarget.GetComponentInParent<MagmaBossAI>();
            if (magma != null) magma.RemoveCurse();

            var ghost = cursedTarget.GetComponentInParent<GhostBossAI>();
            if (ghost != null) ghost.RemoveCurse();

            var swordEnemy = cursedTarget.GetComponentInParent<SkeletonSwordEnemy>();
            if (swordEnemy != null)
            {
                swordEnemy.isCursed = false;
                swordEnemy.curseDamageMult = 1;
                swordEnemy.curseSpeedMult = 1f;
                swordEnemy.curseReflectEnabled = false;
            }
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
        if (cursePreviewVfxPrefab == null || Camera.main == null) { ClearPreview(); return; }
        if (!canCurse || cursedTarget != null || AnyBossCursed()) { ClearPreview(); return; }

        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (!Physics.SphereCast(ray, curseCastRadius, out RaycastHit hit, curseRange, enemyMask, QueryTriggerInteraction.Ignore))
        {
            ClearPreview();
            return;
        }

        DamageRef hitRef = hit.collider.GetComponentInParent<DamageRef>();
        if (hitRef == null || IsAlreadyCursed(hitRef)) { ClearPreview(); return; }

        Transform enemyRoot = GetEnemyRootFromHitRef(hitRef);
        if (enemyRoot == null) { ClearPreview(); return; }

        if (previewTarget != hitRef)
        {
            ClearPreview();
            previewTarget = hitRef;
            Transform follow = getChest(enemyRoot);
            previewInstance = Instantiate(cursePreviewVfxPrefab, follow.position, Quaternion.identity, follow);
            previewInstance.transform.localPosition = Vector3.zero;
            previewInstance.transform.localRotation = Quaternion.identity;
            PlaySound(previewClip, previewVol);
        }
    }

    private void ClearPreview()
    {
        previewTarget = null;
        if (previewInstance != null) { Destroy(previewInstance); previewInstance = null; }
    }

    private bool AnyBossCursed()
    {
        return FindAnyObjectByType<PirateBossAI>()?.isCursed == true ||
               FindAnyObjectByType<MagmaBossAI>()?.isCursed == true ||
               FindAnyObjectByType<GhostBossAI>()?.isCursed == true;
    }

    private bool IsAlreadyCursed(DamageRef target)
    {
        if (target == null) return true;

        var kraken = target.GetComponentInParent<CrackenTentacleCollider>();
        if (kraken != null) return kraken.isCursed;

        var pirate = target.GetComponentInParent<PirateBossAI>();
        if (pirate != null) return pirate.isCursed;

        var magma = target.GetComponentInParent<MagmaBossAI>();
        if (magma != null) return magma.isCursed;

        var ghost = target.GetComponentInParent<GhostBossAI>();
        if (ghost != null) return ghost.isCursed;

        var sword = target.GetComponentInParent<SkeletonSwordEnemy>();
        if (sword != null) return sword.isCursed;

        var gun = target.GetComponentInParent<SkeletonGunEnemy>();
        if (gun != null) return gun.isCursed;

        return false;
    }

    private Transform GetEnemyRootFromHitRef(DamageRef hitRef)
    {
        if (hitRef == null) return null;

        var kraken = hitRef.GetComponentInParent<CrackenTentacleCollider>();
        if (kraken != null) return kraken.transform;

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

        var gun = hitRef.GetComponentInParent<SkeletonGunEnemy>();
        if (gun != null) return gun.transform;

        return hitRef.transform.root;
    }

    private void OnDisable()
    {
        ClearPreview();
    }
}