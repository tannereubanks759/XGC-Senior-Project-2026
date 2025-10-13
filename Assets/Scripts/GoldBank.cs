using UnityEngine;

public class GoldBank : MonoBehaviour
{
    public int gold;

    public void AddGold(int g)
    {
        gold += g;
    }

    public void RemoveGold(int g)
    {
        gold -= g;
    }
}
