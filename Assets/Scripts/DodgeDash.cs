using Unity.VisualScripting;
using UnityEngine;

public class DodgeDash : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Rigidbody rb;
    public Camera cam;
    public int dashforce;
    public int dodgeforce;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Dash()
    {
        rb.AddForce(cam.transform.forward * dashforce * Time.deltaTime, ForceMode.Impulse);
    }
    public void Dodge(Vector3 direction)
    {
        rb.AddForce(direction * dodgeforce * Time.deltaTime, ForceMode.Impulse);
    }
}
