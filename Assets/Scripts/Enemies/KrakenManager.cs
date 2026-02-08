using RayFire;
using UnityEngine;

public class KrakenManager : MonoBehaviour
{
    public Animator headAnim;
    public Animator leftArmAnim;
    public Animator rightArmAnim;
    public int health = 2;
    public KrakenDangerArea[] dangerAreas;
    public RayfireRigid rock;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        dangerAreas = GameObject.FindObjectsByType<KrakenDangerArea>(FindObjectsSortMode.None);
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.K)){ //Debug key to wake up the kraken
            WakeUpKraken();
        }
        if (Input.GetKeyDown(KeyCode.L)){ //Debug key to kill the kraken
            Die();
        }
#endif
    }
    public void TakeDamage()
    {
        health -= 1;
        if(health <= 0)
        {
            Die();
        }
    }
    void Die()
    {
        headAnim.SetTrigger("Dead");
        IslandTeleporter tel = GameObject.FindAnyObjectByType<IslandTeleporter>()?.GetComponent<IslandTeleporter>();
        if (tel != null) tel.OpenDoor();
    }
    public void WakeUpKraken()
    {
        headAnim.SetTrigger("Awake");
        leftArmAnim.SetTrigger("Awake");
        rightArmAnim.SetTrigger("Awake");

        for(int i = 0; i < dangerAreas.Length; i++)
        {
            dangerAreas[i].isAwake = true;
        }

        rock.Demolish();
    }
}
