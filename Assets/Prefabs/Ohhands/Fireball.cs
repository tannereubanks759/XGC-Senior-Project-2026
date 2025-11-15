using UnityEngine;

public class Fireball : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Rigidbody rb;
    public float throwPower = 5f;
    public float splashRadius = 3f;
    public float damage = 30f;
    public GameObject ExplosionPref;
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
                colliders[i].GetComponent<BaseEnemyAI>().TakeDamage((int)damage);
            }
            else if (colliders[i].GetComponent<DamageRef>())
            {
                colliders[i].GetComponent<DamageRef>().TakeDamage((int)damage);
            }
            else if (colliders[i].GetComponent<FirstPersonController>())
            {
                colliders[i].GetComponentInChildren<CombatController>().TakeDamage((int)damage);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Explode();
        Destroy(this.gameObject);
    }
}
