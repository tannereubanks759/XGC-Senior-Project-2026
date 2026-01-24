using UnityEngine;

public class CrackenTentacleCollider : MonoBehaviour
{
    public KrakenTentacle tentacle;
    

    private void OnCollisionEnter(Collision collision)
    {
        if (tentacle.isDropping)
        {
            tentacle.isDropping = false;
            tentacle.GoUp();
        }
    }
}
