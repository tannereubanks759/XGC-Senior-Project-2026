using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthPotion : MonoBehaviour
{
    public KeyCode HealKey = KeyCode.H;
    private int Quantity = 0;
    private float HealAmount = 20f;
    private TextMeshProUGUI text;
    private CombatController healthController;
    private Animator anim;
    void Start()
    {
        anim = FindAnyObjectByType<HealthPotionAnimEvents>().GetComponent<Animator>();
        
        text = GetComponentInChildren<TextMeshProUGUI>();
        text.text = Quantity.ToString();

    }
    public void Update()
    {
        //debug for gaining health potion
        if (Input.GetKeyDown(KeyCode.J))
        {
            CollectHealthPotion();
        }

        if (Input.GetKeyDown(HealKey))
        {
            if(Quantity > 0)
            {
                if(anim != null)
                {
                    anim.SetTrigger("Drink");
                }
                else //if no health controllers exist in the scene
                {
                    Debug.Log("No healthController or CombatController exists in the scene. Check: Is the player active?");
                }
            }
            else
            {
                //Code for not having enough heal potions
            }
        }
    }
    public void CollectHealthPotion()
    {
        SetQuantity(Quantity += 1); //Add one health potion
        SetText(GetQuantity().ToString()); //update the UI
    }

    public void ConsumeHealthPotion()
    {
        SetQuantity(Quantity -= 1);
        SetText(GetQuantity().ToString());
        if (healthController == null)//Allow it to find health controller
        {
            healthController = GameObject.FindAnyObjectByType<CombatController>();
        }
        if (healthController != null)
        {
            ConsumeHealthPotion();
            healthController.Heal((int)GetHealAmount());
        }
    }

    public void SetText(string text) //Why are you looking at this, I know what you're thinking 
    {
        this.text.text = text;
    }
    public void SetHealAmount(float amount)
    {
        HealAmount = amount;
    }
    public float GetHealAmount()
    {
        return HealAmount;
    }
    public void SetQuantity(int quantity)
    {
        Quantity = quantity;
    }
    public int GetQuantity()
    {
        return Quantity;
    }
}
