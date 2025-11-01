using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Blunderbuss : MonoBehaviour
{
    private bool isLoaded;
    public int totalAmmo = 5;
    public int DamagePerPellet = 25;
    public int PelletPerBullet = 4;
    float bulletRadius;
    public LayerMask layers;
    public GameObject ret;
    public GameObject BulletPos;
    public KeyCode shootKey = KeyCode.Mouse0;
    public KeyCode AimKey = KeyCode.Mouse1;
    public Animator anim;
    private inventoryScript Inv;
    [Header("FX")]
    public GameObject PelletHitEffect;
    public GameObject MuzzleFlashParticle;
    public FXPool fxPool; // <-- assign in Inspector
    private TextMeshProUGUI ammoText;
    private WeaponInertia wIntertia;
    public GunSounds sounds;
    void Start()
    {
        ammoText = GameObject.FindGameObjectWithTag("ammoText").GetComponent<TextMeshProUGUI>();
        anim.SetInteger("ammo", totalAmmo);
        isLoaded = true;
        if(ammoText != null)
        {
            ammoText.text = "x" + totalAmmo;
        }
        Inv = GameObject.FindAnyObjectByType<inventoryScript>();
        fxPool = GameObject.FindAnyObjectByType<FXPool>();
        wIntertia = GetComponentInParent<WeaponInertia>();
        isLoaded = true;
        anim.SetBool("canShoot", true);
    }

    private void OnEnable()
    {
        if(Time.timeSinceLevelLoad > 1)
        {
            sounds.PlayEquipSound();
        }
        
        anim.SetInteger("ammo", totalAmmo);
        if (isLoaded) anim.SetBool("canShoot", true);
    }

    void Update()
    {

        if (Input.GetKeyDown(shootKey) && isLoaded && Inv.isOpen == false)
            anim.SetTrigger("Shoot");
        else if (Input.GetKeyDown(shootKey) && Inv.isOpen == false)
        {
            sounds.PlayClickSound();
        }

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            AddAmmo(1);
        }

        anim.SetBool("Aim", Input.GetKey(AimKey));
    }
    public void AddAmmo(int amount)
    {
        sounds.PlayCollectAmmoSound();
        totalAmmo += amount;
        anim.SetInteger("ammo", totalAmmo);
        if (ammoText != null)
        {
            ammoText.text = "x" + totalAmmo;
        }
    }
    // Called by animation event
    void Shoot()
    {
        if(totalAmmo > 0)
        {
            sounds.PlayShootSound();
        }

        // Muzzle flash via pool (3s auto-return)
        if (fxPool && MuzzleFlashParticle)
            fxPool.Spawn(MuzzleFlashParticle, BulletPos.transform.position, BulletPos.transform.rotation, 3f);
        else if (MuzzleFlashParticle)
            Destroy(Instantiate(MuzzleFlashParticle, BulletPos.transform.position, BulletPos.transform.rotation), 3f);

        isLoaded = false;
        anim.SetBool("canShoot", false);

        if (totalAmmo > 0) totalAmmo--;
        anim.SetInteger("ammo", totalAmmo);
        if (ammoText != null)
        {
            ammoText.text = "x" + totalAmmo;
        }

        // ⚠ Using width/2 is usually better than anchoredPosition.x
        // Keep your original if that’s intentional:
        bulletRadius = ret.GetComponent<RectTransform>().anchoredPosition.x;
        
        // Group by damageable component (prevents multi-collider dupes)
        var skullHits = new Dictionary<FloatingSkullAI, int>();
        var gruntHits = new Dictionary<BasicSkeleton, int>();
        var bossHits = new Dictionary<PirateBossAI, int>();
        int pelletsThatHitAnything = 0;

        for (int i = 0; i < PelletPerBullet; i++)
        {
            float randomx = Random.Range(-bulletRadius, bulletRadius);
            float randomy = Random.Range(-bulletRadius, bulletRadius);
            Debug.Log(Input.mousePosition);
            //Vector3 screenPos = Input.mousePosition + (new Vector3(randomx, randomy, 0f));
            Vector2 randomInsideaCircle = Random.insideUnitCircle;
            Vector3 screenPos = Input.mousePosition + (new Vector3(randomInsideaCircle.x, randomInsideaCircle.y, 0f) * bulletRadius);
            Ray ray = Camera.main.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layers))
            {
                pelletsThatHitAnything++;

                // Impact FX
                if (fxPool && PelletHitEffect)
                    fxPool.Spawn(PelletHitEffect, hit.point, Quaternion.FromToRotation(transform.up, hit.normal), 3f);
                else if (PelletHitEffect)
                    Destroy(Instantiate(PelletHitEffect, hit.point, Quaternion.FromToRotation(transform.up, hit.normal)), 3f);

                // Use collider's hierarchy, NOT root tag
                Transform t = hit.collider.attachedRigidbody ? hit.collider.attachedRigidbody.transform : hit.transform;
                
                // Prefer components over tags
                var skull = t.GetComponentInParent<FloatingSkullAI>();
                var grunt = t.GetComponentInParent<BasicSkeleton>();
                var boss = t.GetComponentInParent<PirateBossAI>();
                if (skull)
                {
                    if (skullHits.TryGetValue(skull, out int c)) skullHits[skull] = c + 1;
                    else skullHits[skull] = 1;
                    // Debug
                    Debug.Log($"Pellet hit SKULL: {skull.name} via {hit.collider.name}");
                }
                else if (grunt)
                {
                    if (gruntHits.TryGetValue(grunt, out int c)) gruntHits[grunt] = c + 1;
                    else gruntHits[grunt] = 1;
                    Debug.Log($"Pellet hit ENEMY: {grunt.name} via {hit.collider.name}");
                }
                else if (boss)
                {
                    if (bossHits.TryGetValue(boss, out int c)) bossHits[boss] = c + 1;
                    else bossHits[boss] = 1;
                    ///Debug.Log($"Pellet hit Boss: {grunt.name} via {hit.collider.name}");
                }
                else
                {
                    // Optional: Debug what we hit that isn’t damageable
                    // Debug.Log($"Pellet hit non-damageable: {t.name} (layer {t.gameObject.layer})");
                }
                
            }
        }

        // Apply batched damage
        foreach (var kvp in skullHits)
        {
            int totalDamage = kvp.Value * DamagePerPellet;
            // Your FloatingSkullAI.ApplyDamage must be public
            kvp.Key.ApplyDamage(totalDamage);
            Debug.Log($"Applied {totalDamage} dmg to SKULL {kvp.Key.name} (pellets {kvp.Value})");
        }
        
        foreach (var kvp in gruntHits)
        {
            int totalDamage = kvp.Value * DamagePerPellet;
            kvp.Key.TakeDamage(totalDamage);
            Debug.Log($"Applied {totalDamage} dmg to ENEMY {kvp.Key.name} (pellets {kvp.Value})");
        }
        foreach (var kvp in bossHits)
        {
            int totalDamage = kvp.Value * DamagePerPellet;
            kvp.Key.TakeDamage(totalDamage);
            Debug.Log($"Applied {totalDamage} dmg to ENEMY {kvp.Key.name} (pellets {kvp.Value})");
        }
        

        // Scale recoil by pellets connected (caps at 4)
        if (wIntertia)
            wIntertia.FireRecoil(Mathf.Clamp(pelletsThatHitAnything, 1, 4));
    }


    void SetLoaded()
    {
        isLoaded = true;
        anim.SetBool("canShoot", true);
    }

    // (Optional) still using instantiate/destroy — consider pooling this too later.
    void ShowShotLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("ShotTracer");
        var lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = 0.02f;
        lr.endWidth = 0.02f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.red;
        lr.endColor = Color.red;
        Destroy(lineObj, 0.05f);
    }
}
