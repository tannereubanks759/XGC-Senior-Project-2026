using UnityEngine;

public class CrackenTentacleCollider : MonoBehaviour
{
    public KrakenTentacle tentacle;

    private float nextHit = 0f;
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Player" && Time.time > nextHit && tentacle.isDropping)
        {
            collision.gameObject.GetComponentInChildren<CombatController>().TakeDamageByBoss(30);
            nextHit = Time.time + 5f;
        }
        if (tentacle.isDropping)
        {
            tentacle.isDropping = false;
            tentacle.GoUp();
        }
    }
}
