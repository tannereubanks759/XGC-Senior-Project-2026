using UnityEngine;

public class GoldPile : MonoBehaviour
{
    public int gold;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<GoldBank>().AddGold(gold);
            Destroy(this);
        }
    }
}
