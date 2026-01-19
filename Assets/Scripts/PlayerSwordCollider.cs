using UnityEngine;

public class PlayerSwordCollider : MonoBehaviour
{

    public Collider col;
    

    public void EnableSwordCollider()
    {
        col.enabled = true;
    }
    public void DisableSwordCollider()
    {
        col.enabled = false;
    }
}
