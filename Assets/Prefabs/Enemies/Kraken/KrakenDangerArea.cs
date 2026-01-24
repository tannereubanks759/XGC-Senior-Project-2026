using UnityEngine;

public class KrakenDangerArea : MonoBehaviour
{

    public KrakenTentacle tentacle;

    private void OnTriggerStay(Collider other)
    {
        if(other.tag == "Player")
        {
            tentacle.playerInDangerArea = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.tag == "Player")
        {
            tentacle.playerInDangerArea = false;
        }
    }
}
