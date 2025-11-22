using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class SetOnFire : MonoBehaviour
{
    BaseEnemyAI aiRef; //used for skeletons
    DamageRef dmgRef; //used for bosses
    public ParticleSystem system;
    public bool OnFire = false;
    public bool InLava = false;
    public float lavaDamage = 30f;

    public float timeOnFire = 3f;
    public float tickInterval = 1f;
    public float damagePerTick = 5f;

    private float nextTick = 0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        aiRef = GetComponent<BaseEnemyAI>();
        dmgRef = GetComponent<DamageRef>();
    }

    // Update is called once per frame
    void Update()
    {
        if (InLava && Time.time > nextTick)
        {
            ApplyDamage((int)lavaDamage);
            nextTick = Time.time + tickInterval;
        }
        if (OnFire && Time.time > nextTick) //Logic for being on fire
        {
            ApplyDamage((int)damagePerTick);
            nextTick = Time.time + tickInterval;
        }
    }
    public void SetEnemyOnFire()
    {
        if (this.gameObject.activeSelf)
        {
            StartCoroutine("fireEnum");
        }
        
    }
    void ApplyDamage(int damage)
    {
        if (aiRef)
        {
            aiRef.TakeDamage(damage);
        }
        if (dmgRef)
        {
            dmgRef.TakeDamage(damage);
        }
    }

    IEnumerator fireEnum()
    {
        OnFire = true;
        if (system)
        {
            var emission = system.emission;
            if (system.isStopped)
            {
                system.Play();
            }
            else
            {
                emission.enabled = true;
            }
        }
        yield return new WaitForSeconds(timeOnFire);

        if(system)
        {
            var emission = system.emission;
            emission.enabled = false;
        }
        
        OnFire = false;
        nextTick = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Lava")
        {
            InLava = true;
            if (system)
            {
                var emission = system.emission;
                if (system.isStopped)
                {
                    system.Play();
                }
                else
                {
                    emission.enabled = true;
                }
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.tag == "Lava")
        {
            InLava = false;
            if (system)
            {
                var emission = system.emission;
                emission.enabled = false;
            }
        }
    }
}
