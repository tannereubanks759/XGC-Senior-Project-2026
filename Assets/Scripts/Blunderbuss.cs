using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

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

    public GameObject PelletHitEffect;
    public GameObject MuzzleFlashParticle;
    private WeaponInertia wIntertia;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        wIntertia = GetComponentInParent<WeaponInertia>();
        isLoaded = true;
        anim.SetBool("canShoot", true);
    }
    private void OnEnable()
    {
        if (isLoaded)
        {
            anim.SetBool("canShoot", true);
        }
    }
    // Update is called once per frame
    void Update()
    {
        //shoot functionality
        if(Input.GetKeyDown(shootKey) && isLoaded)
        {
            anim.SetTrigger("Shoot");
        }

        //aim functionality
        if (Input.GetKey(AimKey))
        {
            anim.SetBool("Aim", true);
        }
        else
        {
            anim.SetBool("Aim", false);
        }
    }

void Shoot()
{
    wIntertia.FireRecoil(2);
    Destroy(Instantiate(MuzzleFlashParticle, BulletPos.transform.position, BulletPos.transform.rotation), 3f);
    isLoaded = false;
    anim.SetBool("canShoot", false);
    totalAmmo--;

    //spread based on your reticle
    bulletRadius = ret.GetComponent<RectTransform>().anchoredPosition.x / 10f;

    //Collect pellet hits by target
    var hitsByTarget = new Dictionary<GameObject, int>();

    for (int i = 0; i < PelletPerBullet; i++)
    {
        float randomx = Random.Range(-bulletRadius, bulletRadius);
        float randomy = Random.Range(-bulletRadius, bulletRadius);
        Vector3 screenPos = Input.mousePosition + new Vector3(randomx, randomy, 0f);
        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layers))
        {
            Destroy(Instantiate(PelletHitEffect, hit.point, Quaternion.FromToRotation(transform.up, hit.normal)), 3f);

            //Find the canonical GameObject for this target (rigidbody root or transform root)
            GameObject targetGO = hit.rigidbody ? hit.rigidbody.gameObject : hit.transform.root.gameObject;

            //Only count things we intend to damage
            if (targetGO.CompareTag("Enemy") || targetGO.CompareTag("Skull"))
            {
                if (hitsByTarget.TryGetValue(targetGO, out int count))
                    hitsByTarget[targetGO] = count + 1;
                else
                    hitsByTarget[targetGO] = 1;
            }

            //(Optional) draw tracers etc.
            //ShowShotLine(BulletPos.transform.position, hit.point);
        }
    }

    //Apply a single damage call per target (hits * DamagePerPellet)
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
}

void SetLoaded()
    {
        isLoaded = true;
        anim.SetBool("canShoot", true);
    }
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

        Destroy(lineObj, 0.05f); // disappear quickly like a tracer
    }

}
