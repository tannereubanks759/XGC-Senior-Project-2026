using JetBrains.Annotations;
using UnityEngine;

public class TreasureRoomLockKey : MonoBehaviour
{

    public bool PlayerHasKey = false;
    public GameObject lockObj;
    public GameObject keyObj;
    public GameObject ArenaGate;
    private void Start()
    {
        ArenaGate.SetActive(true);
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            UnlockArena(); //Helper to test the function
        }
    }
    public void UnlockArena()
    {
        ArenaGate.SetActive(false);
    }
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
