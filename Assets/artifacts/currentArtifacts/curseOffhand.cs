using UnityEngine;
using UnityEngine.AI;

public class curseOffhand : MonoBehaviour
{
    public bool isActive;
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
        if (!isActive)
        {
            return;
        }
           
        if (cursedEnemy != null && cursedEnemy.currentHealth <= 0)
        {
            cursedEnemy = null;

        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            
            if (cursedEnemy != null)
            {
                return;
            }

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
            }
        }
    }
}
