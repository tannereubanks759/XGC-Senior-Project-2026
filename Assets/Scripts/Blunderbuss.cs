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

    public GameObject PelletHitEffect;
    public GameObject MuzzleFlashParticle;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
        Destroy(Instantiate(MuzzleFlashParticle, BulletPos.transform.position, BulletPos.transform.rotation), 3f);
        isLoaded = false;
        anim.SetBool("canShoot", false);
        totalAmmo--;
        bulletRadius = ret.GetComponent<RectTransform>().anchoredPosition.x / 10;
        //Debug.Log(bulletRadius);
        for (int i = 0; i < PelletPerBullet; i++)
        {
            float randomx = Random.Range(-bulletRadius, bulletRadius);
            float randomy = Random.Range(-bulletRadius, bulletRadius);
            Vector3 ScreenPos = Input.mousePosition + new Vector3(randomx, randomy, 0);
            Ray Ray = Camera.main.ScreenPointToRay(ScreenPos);
            
            if (Physics.Raycast(Ray, out RaycastHit hit, Mathf.Infinity, layers))
            {
                //ShowShotLine(BulletPos.transform.position, hit.point);

                Destroy(Instantiate(PelletHitEffect, hit.point, Quaternion.FromToRotation(transform.up, hit.normal)), 3f);
                /*
                // Damage what was hit
                IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(10);
                }
                */
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
