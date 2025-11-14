using UnityEngine;

public class FireballManager : MonoBehaviour
{
    public KeyCode useKey = KeyCode.F;
    public GameObject FireballPref;
    public GameObject fireball_1;
    public GameObject fireball_2;
    public int activeFireballs = 2;
    public float scalePower = .01f;
    private Vector3 targetScale;
    private bool fireball_1_active = true;
    private bool fireball_2_active = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        targetScale = fireball_1.gameObject.transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        if(fireball_1.transform.localScale.x < targetScale.x)
        {
            fireball_1.transform.localScale += new Vector3(1, 1, 1) * Time.deltaTime * scalePower;
            if(fireball_1.transform.localScale.x >= targetScale.x)
            {
                fireball_1_active = true;
                activeFireballs++;
            }
        }
        if(fireball_2.transform.localScale.x < targetScale.x)
        {
            fireball_2.transform.localScale += new Vector3(1, 1, 1) * Time.deltaTime * scalePower;
            if (fireball_2.transform.localScale.x >= targetScale.x)
            {
                fireball_2_active = true;
                activeFireballs++;
            }
        }

        if (Input.GetKeyDown(useKey) && activeFireballs > 0)
        {
            Throw();
        }
    }

    void Throw()
    {
        activeFireballs--;
        int random = Random.Range(0, 2);
        if(random == 0)
        {
            if (fireball_1_active == true)
            {
                fireball_1_active = false;
                ActivateThrow(fireball_1);
            }
            else
            {
                fireball_2_active = false;
                ActivateThrow(fireball_2);
            }
        }
        if(random == 1)
        {
            if (fireball_2_active == true)
            {
                fireball_2_active = false;
                ActivateThrow(fireball_2);
            }
            else
            {
                fireball_1_active = false;
                ActivateThrow(fireball_1);
            }
        }

    }

    void ActivateThrow(GameObject obj)
    {
        Destroy(Instantiate(FireballPref, obj.transform.position, Camera.main.transform.rotation), 5f);
        obj.transform.localScale = Vector3.zero;
    }
}
