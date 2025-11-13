using UnityEngine;

public class curseOffhand : MonoBehaviour
{
    public bool isActive;
    private BaseEnemyAI cursedEnemy;
    private int curseRange = 10;
    public LayerMask enemyMask;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isActive) 
        { 
            if(Input.GetKeyDown(KeyCode.F))
            {
                if(cursedEnemy!=null) 
                {
                    return;
                }
                Ray curseRay= new Ray(Camera.main.transform.position, Camera.main.transform.forward);
                if (Physics.Raycast(curseRay, out RaycastHit hit, curseRange, enemyMask))
                {
                    BaseEnemyAI enemy = hit.collider.GetComponentInParent<BaseEnemyAI>();
                    if (enemy != null)
                    {
                        Debug.Log("Applied curse");
                        enemy.gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}
