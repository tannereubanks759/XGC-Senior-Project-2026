using UnityEngine;

public class KrakenDangerArea : MonoBehaviour
{

    public KrakenTentacle tentacle;
    public bool isAwake = false;
    public RaiseLowerMover krakenWalls;
    public KrakenManager km;
    public static bool wallsRaised = false;
    private void Start()
    {
        isAwake = false;
        wallsRaised = false;
    }
    private void OnTriggerStay(Collider other)
    {
        if (!isAwake) return;
        if (km.health <= 0) return;
        if(other.tag == "Player")
        {
            tentacle.playerInDangerArea = true;

            if (krakenWalls.isRaised == false && wallsRaised == false)
            {
                krakenWalls.Raise();
                wallsRaised = true;
            }
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
