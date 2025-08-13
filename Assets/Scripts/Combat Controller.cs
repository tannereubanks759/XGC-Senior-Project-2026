using System.Runtime.CompilerServices;
using UnityEngine;

public class CombatController : MonoBehaviour
{
    [Header("Controls")]
    public KeyCode primaryAttack = KeyCode.Mouse0;
    public KeyCode block_or_aim = KeyCode.Mouse1; 
    public KeyCode dodge = KeyCode.Space; //must be holding block key


    [Header("Animation")]
    public Animator swordAnim;
    private bool swinging;
    private bool blocking;

    //Private
    private DodgeDash dodgeScript;
    private Rigidbody rb;
    private FirstPersonController controller;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        dodgeScript = GetComponentInChildren<DodgeDash>();
        rb = GetComponentInParent<Rigidbody>();
        controller = rb.GetComponent<FirstPersonController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(primaryAttack))
        {
            swinging = true;
            swordAnim.SetBool("swinging", true);
        }
        else
        {
            swinging = false;
            swordAnim.SetBool("swinging", false);
        }


        if (Input.GetKey(block_or_aim))
        {
            blocking = true;
            swordAnim.SetBool("blocking", true);
        }
        else
        {
            blocking = false;
            swordAnim.SetBool("blocking", false);
        }

        if(swinging && blocking) //heavy attack
        {
            swordAnim.SetBool("heavy", true);
            controller.playerCanMove = false;
        }
        else
        {
            swordAnim.SetBool("heavy", false);
            controller.playerCanMove = true;
        }

        if(blocking && !swinging && Input.GetKeyDown(dodge))
        {
            Vector3 direction = rb.linearVelocity.normalized;
            dodgeScript.Dodge(direction);
        }

        
        
    }
}
