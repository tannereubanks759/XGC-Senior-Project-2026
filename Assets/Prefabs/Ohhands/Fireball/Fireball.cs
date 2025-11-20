using UnityEngine;

public class Fireball : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Rigidbody rb;
    public float throwPower = 5f;
    public float splashRadius = 3f;
    public float damage = 30f;
    public GameObject ExplosionPref;
    public bool setEnemiesOnFire = false;
    void Start()
    {
        rb.AddForce(this.transform.forward * throwPower, ForceMode.Impulse);
    }

    void Explode()
    {
        Destroy(Instantiate(ExplosionPref, this.transform.position, Quaternion.identity), 3f);
        Collider[] colliders = Physics.OverlapSphere(this.transform.position, splashRadius);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].GetComponent<BaseEnemyAI>())
            {
                BaseEnemyAI ai = colliders[i].GetComponent<BaseEnemyAI>();
                ai.TakeDamage((int)damage);
                if(ai!= null && ai.GetComponent<SetOnFire>() && setEnemiesOnFire)
                {
                    ai.GetComponent<SetOnFire>().SetEnemyOnFire();
                }
            }
            else if (colliders[i].GetComponent<DamageRef>())
            {
                DamageRef ai = colliders[i].GetComponent<DamageRef>();
                ai.TakeDamage((int)damage);
                if (ai != null && ai.GetComponent<SetOnFire>() && setEnemiesOnFire)
                {
                    ai.GetComponent<SetOnFire>().SetEnemyOnFire();
                }
            }
            else if (colliders[i].GetComponent<FirstPersonController>())
            {
                CombatController player = colliders[i].GetComponentInChildren<CombatController>();
                if (Vector3.Distance(this.gameObject.transform.position, player.gameObject.transform.position) < splashRadius / 2)
                {
                    player.TakeDamage((int)damage);
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Explode();
        Destroy(this.gameObject);
    }
}
