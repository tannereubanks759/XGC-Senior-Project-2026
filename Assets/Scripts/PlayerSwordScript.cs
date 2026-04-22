using UnityEngine;
using static Unity.Collections.AllocatorManager;

public class PlayerSwordScript : MonoBehaviour
{
    public AudioSource source;
    public AudioClip[] hitSkeleton;
    public AudioClip[] hitKraken1;
    public AudioClip[] hitKraken2;
    public GameObject boneChips;
    public GameObject ClashEffect;
    public AudioClip chainLightningStart;
    public float chainRadius = 6f;
    [Range(0f, 1f)] public float chainDamageMultiplier = 0.5f;
    public GameObject lightningBoltPrefab;
    public swordDamageDeterminer swordDamage;
    public Collider col;
    public int damage = 10;
    public chargeBaseScript charge;
    public SwordSounds swordSounds;
    //private int currentChain = 0;
    public int maxSkeletonChaining = 3;
    public float chargePerHit = 10f;
    public void PlaySound(AudioClip clip)
    {
        source.PlayOneShot(clip, 1f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Enemy" || other.tag == "Boss")
        {
            Transform enemyRoot = other.transform.root;
            SkeletonSwordEnemy skeleton = other.GetComponent<SkeletonSwordEnemy>();
            PirateBossAI boss = other.GetComponentInParent<PirateBossAI>();
            bool blocked = (skeleton != null && skeleton.isBlocking);
            if (other.gameObject.layer == 22) 
            {
                PlaySound(hitKraken1[Random.Range(0, hitKraken1.Length)]);
                PlaySound(hitKraken2[Random.Range(0, hitKraken2.Length)]);

                col.enabled = false;
                other.GetComponent<DamageRef>().TakeDamage(damage);
            }
            else if (skeleton != null && ClashEffect && skeleton.isBlocking == true)
            {
                Collider enemySword = skeleton.GetComponentInChildren<SkeletonAnimEvents>().swordCol;
                Destroy(Instantiate(ClashEffect, enemySword.ClosestPoint(this.transform.position), Quaternion.identity), 3);
                col.enabled = false;
                other.GetComponent<DamageRef>().TakeDamage(damage);
            }
            else if (boss && boss.State == PirateBossAI.BossState.Block)
            {
                Collider AnchorCol = boss.GetComponentInChildren<AnchorWeapon>().GetComponent<Collider>();
                Destroy(Instantiate(ClashEffect, AnchorCol.ClosestPoint(this.transform.position), Quaternion.identity), 3);
                other.GetComponent<DamageRef>().TakeDamage(damage);
                swordSounds.PlayClashSound();
            }
            else //Didnt hit basic skeleton
            {
                Destroy(Instantiate(boneChips, other.ClosestPoint(this.transform.position), Quaternion.identity), 3);
                PlaySound(hitSkeleton[Random.Range(0, hitSkeleton.Length)]);

                col.enabled = false;
                other.GetComponent<DamageRef>().TakeDamage(damage);
            }
            // increase charge 
            if (swordDamage != null && swordDamage.isLighting && charge != null && !blocked)
            {
                charge.increaseCharge(chargePerHit);
            }
            // chaining
            if (swordDamage != null && swordDamage.isLighting && !blocked)
            {
                ChainLightning(other.transform, damage);
            }

        }
        if(other.tag == "barrel")
        {
            other.GetComponent<DamageRef>().TakeDamage(1);
        }
        
        
    }
    private void ChainLightning(Transform firstTarget, int baseDamage)
    {
        if (firstTarget == null || lightningBoltPrefab == null) return;
        source.PlayOneShot(chainLightningStart, 0.9f);
        int chainedDamage = Mathf.RoundToInt(baseDamage * chainDamageMultiplier);
        Transform lastDamaged = firstTarget;
        Collider[] closeEnemies = Physics.OverlapSphere(firstTarget.position, chainRadius, ~0, QueryTriggerInteraction.Ignore);
        int currentChain = 0;
        foreach (Collider c in closeEnemies)
        {
            //limit of chaining
            if (currentChain > maxSkeletonChaining)
            {
                break;
            }
            if (!c.CompareTag("Enemy")) continue;
            if (c.transform == firstTarget) continue;
            DamageRef dr = c.GetComponent<DamageRef>();
            if (dr == null) continue;
            dr.TakeDamage(chainedDamage);
            //offset 
            Vector3 offset = Vector3.up * 1.2f;
            Transform startAnchor = new GameObject("lastDamagedPoint").transform;
            Transform endAnchor = new GameObject("enemyEnderPoint").transform;
            startAnchor.position = lastDamaged.position + offset;
            endAnchor.position = c.transform.position + offset;
            SpawnLightningArc(startAnchor, endAnchor);
            Destroy(startAnchor.gameObject, 0.1f);
            Destroy(endAnchor.gameObject, 0.1f);
            lastDamaged = c.transform;
            currentChain++;
        }
    }
    private void SpawnLightningArc(Transform start, Transform end)
    {
        var lightning = Instantiate(lightningBoltPrefab);
        MonoBehaviour bolt = null;

        foreach (var script in lightning.GetComponents<MonoBehaviour>())
        {
            if (script.GetType().Name == "LightningBoltPrefabScript")
            {
                bolt = script;
                break;
            }
        }

        if (bolt == null) return;

        var t = bolt.GetType();
        var fSource = t.GetField("Source");
        var fDest = t.GetField("Destination");
        if (fSource != null) fSource.SetValue(bolt, start.gameObject);
        if (fDest != null) fDest.SetValue(bolt, end.gameObject);
    }
}

