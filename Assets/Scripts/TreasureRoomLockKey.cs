
using UnityEngine;

public class TreasureRoomLockKey : MonoBehaviour
{

    public bool PlayerHasKey = false;
    public GameObject lockObj;
    public GameObject keyObj;
    
    public void Unlock()
    {
        Debug.Log("Unlock");
        if (PlayerHasKey)
        {
            lockObj.SetActive(false);
            interactScript x = GameObject.FindAnyObjectByType<interactScript>();
            if (x != null) //Can find interact script
            {
                x.treasureRoomUnlocked = true;
            }
        }
    }
    public void PickupKey()
    {
        PlayerHasKey = true;
        keyObj.SetActive(false);
    }
}
