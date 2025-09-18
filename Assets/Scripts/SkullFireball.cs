using UnityEngine;

public class SkullFireball : MonoBehaviour
{
    [Header("Projectile")]
    public float speed = 18f;
    public int damage = 10;
    public float lifeTime = 6f;
    public LayerMask hitLayers;         // Who can be hit/damaged
    public GameObject hitVFX;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player")
        {
            other.gameObject.GetComponentInChildren<CombatController>().TakeDamage(damage);
        }

        if (hitVFX) Instantiate(hitVFX, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
