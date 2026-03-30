using UnityEngine;

public class LavaDamage : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("Lava Damage Settins")]
    public float timeInBetweenEachTick = 1f;
    public float damage;
    public CombatController health;
    public bool inLava = false;
    public bool inInk = false;

    private float nextTick = 0f;
    private float nextInkTick = 0f;
    
    // Update is called once per frame
    void Update()
    {
        if(inLava && Time.time > nextTick)
        {
            nextTick = Time.time + timeInBetweenEachTick;
            health.TakeDamage((int) damage); 
        }
        if(inInk && Time.time > nextInkTick)
        {
            nextInkTick = Time.time + timeInBetweenEachTick / 2;
            health.TakeDamage((int) damage / 6); 
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "Lava")
        {
            inLava = true;
        }
        if (other.tag == "ink")
        {
            inInk = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.tag == "Lava")
        {
            inLava = false;
        }
        if(other.tag == "ink")
        {
            inInk = false;
        }
    }
    private void OnParticleCollision(GameObject other)
    {
        if(other.gameObject.layer == 17)//lava layer
        {
            if(Time.time > nextTick)
            {
                nextTick = Time.time + timeInBetweenEachTick;
                health.TakeDamage((int)damage);
            }
        }
    }
}
