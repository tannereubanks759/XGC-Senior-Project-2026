using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Blunderbuss : MonoBehaviour
{
    private bool isLoaded;
    public int totalAmmo = 40;
    public int DamagePerPellet = 25;
    public int PelletPerBullet = 4;
    private float bulletRadius;
    public LayerMask layers;
    public GameObject ret;
    public GameObject BulletPos;
    public KeyCode shootKey = KeyCode.Mouse0;
    public KeyCode AimKey = KeyCode.Mouse1;
    public Animator anim;

    [Header("FX")]
    public GameObject PelletHitEffect;
    public GameObject MuzzleFlashParticle;
    public FXPool fxPool; // <-- assign in Inspector

    private WeaponInertia wIntertia;

    void Start()
    {
        fxPool = GameObject.FindAnyObjectByType<FXPool>();
        wIntertia = GetComponentInParent<WeaponInertia>();
        isLoaded = true;
        anim.SetBool("canShoot", true);
    }

    private void OnEnable()
    {
        if (isLoaded) anim.SetBool("canShoot", true);
    }

    void Update()
    {
        if (Input.GetKeyDown(shootKey) && isLoaded)
            anim.SetTrigger("Shoot");

        anim.SetBool("Aim", Input.GetKey(AimKey));
    }

    // Called by animation event
    void Shoot()
    {
        // Muzzle flash via pool (3s auto-return)
        if (fxPool && MuzzleFlashParticle)
            fxPool.Spawn(MuzzleFlashParticle, BulletPos.transform.position, BulletPos.transform.rotation, 3f);
        else if (MuzzleFlashParticle) // fallback if pool not assigned
            Destroy(Instantiate(MuzzleFlashParticle, BulletPos.transform.position, BulletPos.transform.rotation), 3f);

        isLoaded = false;
        anim.SetBool("canShoot", false);
        totalAmmo--;

        bulletRadius = ret.GetComponent<RectTransform>().anchoredPosition.x / 10f;

        var hitsByTarget = new Dictionary<GameObject, int>();

        for (int i = 0; i < PelletPerBullet; i++)
        {
            float randomx = Random.Range(-bulletRadius, bulletRadius);
            float randomy = Random.Range(-bulletRadius, bulletRadius);
            Vector3 screenPos = Input.mousePosition + new Vector3(randomx, randomy, 0f);
            Ray ray = Camera.main.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layers))
            {
                // Impact FX via pool (3s auto-return)
                if (fxPool && PelletHitEffect)
                {
                    fxPool.Spawn(
                        PelletHitEffect,
                        hit.point,
                        Quaternion.FromToRotation(transform.up, hit.normal),
                        3f
                    );
                }
                else if (PelletHitEffect) // fallback if pool not assigned
                {
                    Destroy(Instantiate(PelletHitEffect, hit.point, Quaternion.FromToRotation(transform.up, hit.normal)), 3f);
                }

                GameObject targetGO = hit.rigidbody ? hit.rigidbody.gameObject : hit.transform.root.gameObject;

                if (targetGO.CompareTag("Enemy") || targetGO.CompareTag("Skull"))
                {
                    if (hitsByTarget.TryGetValue(targetGO, out int count))
                        hitsByTarget[targetGO] = count + 1;
                    else
                        hitsByTarget[targetGO] = 1;
                }
            }
        }

        // Batch damage once per target
        foreach (var kvp in hitsByTarget)
        {
            GameObject target = kvp.Key;
            int hits = kvp.Value;
            int totalDamage = hits * DamagePerPellet;

            if (target.CompareTag("Enemy"))
            {
                var grunt = target.GetComponentInParent<GruntEnemyAI>();
                if (grunt != null) grunt.TakeDamage(totalDamage);
            }
            else if (target.CompareTag("Skull"))
            {
                var skull = target.GetComponentInParent<FloatingSkullAI>();
                if (skull != null) skull.ApplyDamage(totalDamage);
            }
        }

        // Scale recoil by # of pellets connected (caps at 4)
        if (wIntertia)
            wIntertia.FireRecoil(Mathf.Clamp(hitsByTarget.Sum(kv => kv.Value), 1, 4));
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
