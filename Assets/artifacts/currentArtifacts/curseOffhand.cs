using UnityEngine;
using UnityEngine.AI;

public class curseOffhand : MonoBehaviour
{
    //public bool isActive;
    public BaseEnemyAI cursedEnemy;
    private int curseRange = 10;
    public LayerMask enemyMask;
    public int damageMult = 2;
    public bool slowUpgrade = false;
    public bool reflectionUpgrade = false;
    private float slowedSpeed;
    
    //private BaseEnemyAI baseAI;
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
        NavMeshAgent navMesh = baseAI.GetComponent<NavMeshAgent>();
        if(slowUpgrade) 
        {
            baseAI.speedMultiplier = .25f;
        }
        /*if(reflectionUpgrade) 
        { 

        }*/
    }
    // Update is called once per frame
    void Update()
    {
        
           
        if (cursedEnemy != null && cursedEnemy.currentHealth <= 0)
        {
            cursedEnemy = null;

        }
        //only run when f is pressed
        if (!Input.GetKeyDown(KeyCode.F))
        {
            return;
        }
            //make sure two thingsd arent cursed at once
        if (cursedEnemy != null || FindAnyObjectByType<PirateBossAI>()?.isCursed == true || FindAnyObjectByType<MagmaBossAI>()?.isCursed == true)
        {
            return;
        }
        // base enemy logic
        Ray curseRay = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            if (Physics.Raycast(curseRay, out RaycastHit hit, curseRange, enemyMask))
            {
                BaseEnemyAI enemy = hit.collider.GetComponentInParent<BaseEnemyAI>();
                if (enemy != null)
                {
                    Debug.Log("Applied curse");
                    var enemyScript = enemy.GetComponent<BaseEnemyAI>();
                    cursedEnemy = enemyScript;
                    enemyScript.damageMult = damageMult;
                    Vector3 offset = new Vector3(0f, 1.3f, 0f);
                    var vfx = Instantiate(enemyScript.curseVfxPrefab, enemy.transform.position + offset, Quaternion.identity, enemy.transform);
                    vfx.transform.localPosition = offset;
                    checkUpgrade(enemyScript);
                }
                //boss enemy logic
                else
                {
                    DamageRef bossRef = hit.collider.GetComponentInParent<DamageRef>();
                    if (bossRef != null)
                    {
                        PirateBossAI pirateboss = bossRef.GetComponentInParent<PirateBossAI>();
                        MagmaBossAI magmaBoss = bossRef.GetComponentInParent<MagmaBossAI>();

                        if (pirateboss != null)
                        {
                            if (!pirateboss.isCursed)
                            {
                                Vector3 offset = new Vector3(0f, 2f, 0f);
                                var vfx = Instantiate(pirateboss.cursedVfxPrefab, pirateboss.transform.position + offset, Quaternion.identity, pirateboss.transform);
                                vfx.transform.localPosition = offset;
                            }
                        pirateboss.curseBoss(slowUpgrade, reflectionUpgrade);
                        }
                    //magma boss logic
                    else if (magmaBoss != null)
                    {
                        if (!magmaBoss.isCursed)
                        {
                            Vector3 offset = new Vector3(0f, 1.6f, 0f);
                            var vfx = Instantiate(magmaBoss.cursedVfxPrefab, magmaBoss.transform.position + offset, Quaternion.identity, magmaBoss.transform);
                            vfx.transform.localPosition = offset;
                        }

                        magmaBoss.CurseBoss(slowUpgrade, reflectionUpgrade);
                    }
                }
            }
        }
    }
}
        
    

