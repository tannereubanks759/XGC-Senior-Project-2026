using UnityEngine;

public class PlayerSwordCollider : MonoBehaviour
{

    public Collider col;
    private Animator anim;

    private void Start()
    {
        anim = this.GetComponent<Animator>();
    }
    public void DisableStaggered()
    {
        anim.SetBool("Staggered", false);
    }

    public void EnableSwordCollider()
    {
        col.enabled = true;
    }
    public void DisableSwordCollider()
    {
        col.enabled = false;
    }
}
