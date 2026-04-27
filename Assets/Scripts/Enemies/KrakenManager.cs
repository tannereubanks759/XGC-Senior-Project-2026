using RayFire;
using UnityEngine;

public class KrakenManager : MonoBehaviour
{
    public Animator headAnim;
    public Animator leftArmAnim;
    public Animator rightArmAnim;
    public float health = 400;
    public KrakenDangerArea[] dangerAreas;
    public RayfireRigid rock;
    public BossHealthbar healthbar;
    public string bossName = "The Kraken";
    public Color KrakenHealthBarColor;
    public RaiseLowerMover krakenWalls;
    public AudioSource mouthSource;
    public AudioClip deathClip;
    public GameObject RockSounds;
    public Collider[] damageColliders;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        damageColliders[1].enabled = false;
        damageColliders[0].enabled = false;
        dangerAreas = GameObject.FindObjectsByType<KrakenDangerArea>(FindObjectsSortMode.None);
    }

    // Update is called once per frame
    void Update()
    {
//#if UNITY_EDITOR
//        if (Input.GetKeyDown(KeyCode.K)){ //Debug key to wake up the kraken
//            WakeUpKraken();
//        }
//        if (Input.GetKeyDown(KeyCode.L)){ //Debug key to kill the kraken
//            Die();
//        }
//#endif
    }
    public void TakeDamage(float dmg)
    {
        Debug.Log("Kraken took " + dmg + " damage");
        
        health -= dmg;
        healthbar.TakeDamage((int) health);
        if (health <= 0)
        {
            Die();
        }
    }
    void Die()
    {
        var bossArenaMusicTrigger = GameObject.FindAnyObjectByType<BossArenaMusicTrigger>();
        if (bossArenaMusicTrigger != null)
        {
            bossArenaMusicTrigger.OnBossDied();
        }
        headAnim.SetTrigger("Dead");
        mouthSource.PlayOneShot(deathClip);
        krakenWalls.Lower();
        levelSavingManager.Instance.ClearCheckpoint();
        IslandTeleporter tel = GameObject.FindAnyObjectByType<IslandTeleporter>()?.GetComponent<IslandTeleporter>();
        if (tel != null) tel.OpenDoor();
    }
    public void WakeUpKraken()
    {
        healthbar = GameObject.FindAnyObjectByType<BossHealthbar>();
        if (!healthbar) return;
        damageColliders[1].enabled = true;
        damageColliders[0].enabled = true;
        RockSounds.SetActive(true);
        mouthSource.Play();
        healthbar.maxHealth = (int) health;
        healthbar.currentHealth = (int) health;
        healthbar.text.text = bossName;
        healthbar.text.color = KrakenHealthBarColor;
        healthbar.fill.color = KrakenHealthBarColor;
        //healthbar.ShowHealthbarOnBossTriggered();
        healthbar.ResetHealthbar();
        healthbar.ShowHealthbarOnBossTriggered();
        healthbar.bossHealthGroup.alpha = 1f;

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
