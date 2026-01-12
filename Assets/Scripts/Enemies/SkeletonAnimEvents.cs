using UnityEngine;

public class SkeletonAnimEvents : MonoBehaviour
{
    public Collider swordCol;
    
    public void EnableSwordCollider()
    {
        swordCol.enabled = true;
    }
    public void DisableSwordCollider()
    {
        swordCol.enabled = false;
    }
}
