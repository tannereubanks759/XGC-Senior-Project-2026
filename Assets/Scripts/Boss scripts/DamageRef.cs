using UnityEngine;

public class DamageRef : MonoBehaviour
{
    private PirateBossAI anchorBoss;
    private MagmaBossAI magmaBoss; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anchorBoss = GetComponent<PirateBossAI>();
        magmaBoss = GetComponent<MagmaBossAI>();
    }

    public void TakeDamage(float damage)
    {
        if (anchorBoss)
        {
            anchorBoss.TakeDamage((int) damage);
        }
        if (magmaBoss)
        {
            magmaBoss.TakeDamage((int)damage);
        }
    }
}
