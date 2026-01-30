using UnityEngine;

public class SkeletonAnimEvents : MonoBehaviour
{
    public Collider swordCol;
    
    public void EnableSwordCollider()
    {
        if(swordCol != null)
        {
            swordCol.enabled = true;
        }
        
    }
    public void DisableSwordCollider()
    {
        if (swordCol != null)
        {
            swordCol.enabled = false;
        }
    }
}
