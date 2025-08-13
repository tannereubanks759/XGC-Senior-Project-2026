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
    public Transform BulletPos;
    public KeyCode shootKey = KeyCode.Mouse0;
    public KeyCode AimKey = KeyCode.Mouse1;
    public Animator anim;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isLoaded = true;
    }

    // Update is called once per frame
    void Update()
    {
        //shoot functionality
        if(Input.GetKey(shootKey) && isLoaded)
        {
            Shoot();
        }

        //aim functionality
        if (Input.GetKey(AimKey))
        {

        }
        else
        {

        }
    }

    void Shoot()
    {
        totalAmmo--;
        bulletRadius = ret.GetComponent<RectTransform>().anchoredPosition.x;
        //Debug.Log(bulletRadius);
        for (int i = 0; i < PelletPerBullet; i++)
        {
            float randomx = Random.Range(-bulletRadius, bulletRadius);
            float randomy = Random.Range(-bulletRadius, bulletRadius);
            Vector3 ScreenPos = Input.mousePosition + new Vector3(randomx, randomy, 0);
            Ray Ray = Camera.main.ScreenPointToRay(ScreenPos);
            if (Physics.Raycast(Ray, out RaycastHit hit, layers))
            {
                Debug.Log(hit.ToString());
            }
        }
        
    }
}
