using UnityEngine;

public class KrakenDangerArea : MonoBehaviour
{

    public KrakenTentacle tentacle;
    public bool isAwake = false;
    private void Start()
    {
        isAwake = false;
    }
    private void OnTriggerStay(Collider other)
    {
        if (!isAwake) return;
        if(other.tag == "Player")
        {
            tentacle.playerInDangerArea = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (!isAwake) return;
        if(other.tag == "Player")
        {
            tentacle.playerInDangerArea = false;
        }
    }
}
