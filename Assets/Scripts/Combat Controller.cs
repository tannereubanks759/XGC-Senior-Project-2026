using UnityEngine;

public class CombatController : MonoBehaviour
{
    [Header("Controls")]
    public KeyCode primaryAttack = KeyCode.Mouse0;
    public KeyCode block_or_aim = KeyCode.Mouse1; 
    public KeyCode dodge = KeyCode.Space; //must be holding block key

    [Header("Stats")]
    public int health = 100;

    [Header("Animation")]
    public Animator swordAnim;
    private bool swinging;
    private bool blocking;




    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
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
        }
        else
        {
            swordAnim.SetBool("heavy", false);
        }
        
    }
}
