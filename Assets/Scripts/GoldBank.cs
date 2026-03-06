using TMPro;
using UnityEngine;

public class GoldBank : MonoBehaviour
{
    public int gold;

    public TextMeshProUGUI goldText;

    private void Start()
    {
        
        UpdateGold();
    }

    public void AddGold(int g)
    {
        gold += g;
        UpdateGold();
    }

    public void RemoveGold(int g)
    {
        gold -= g;
        UpdateGold();
    }

    public void UpdateGold()
    {
        goldText.text = gold.ToString();
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.B)) 
        {
            AddGold(10);
        }
    }
}
