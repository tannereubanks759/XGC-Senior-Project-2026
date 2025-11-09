using UnityEngine;

public class LavaDamage : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("Lava Damage Settins")]
    public float timeInBetweenEachTick = 1f;
    public float damage;
    public CombatController health;
    public bool inLava = false;

    private float nextTick = 0f;

    // Update is called once per frame
    void Update()
    {
        if(inLava && Time.time > nextTick)
        {
            nextTick = Time.time + timeInBetweenEachTick;
            health.TakeDamage((int) damage); 
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "Lava")
        {
            inLava = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.tag == "Lava")
        {
            inLava = false;
        }
    }
}
