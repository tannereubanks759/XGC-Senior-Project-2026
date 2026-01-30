using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthPotion : MonoBehaviour
{
    public KeyCode HealKey = KeyCode.H;
    private int Quantity = 0;
    private float HealAmount = 33.34f;
    private TextMeshProUGUI text;
    private CombatController healthController;
    private Animator anim;
    private WeaponsManager weaponsManager;
    public AudioSource source;
    public AudioClip collectPotion;
    public AudioClip drinkPotion;
    public AudioClip corkPop;
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
            if (healthController == null)//Allow it to find health controller
            {
                healthController = GameObject.FindAnyObjectByType<CombatController>();
            }
            if (Quantity > 0 && healthController != null && healthController.health < 100)
            {
                if(anim != null) //Consume a potion (start animation and disable weapons)
                {
                    if (weaponsManager == null)
                    {
                        weaponsManager = FindAnyObjectByType<WeaponsManager>();
                    }
                    if (weaponsManager != null)
                    {
                        weaponsManager.SetHealing(true);
                    }
                    source.PlayOneShot(corkPop);
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
        source.PlayOneShot(collectPotion);
        SetQuantity(Quantity += 1); //Add one health potion
        SetText(GetQuantity().ToString()); //update the UI
    }

    public void ConsumeHealthPotion()
    {
        source.PlayOneShot(drinkPotion);
        if(weaponsManager == null)
        {
            weaponsManager = FindAnyObjectByType<WeaponsManager>();
        }
        if (weaponsManager != null)
        {
            weaponsManager.SetHealing(false);
        }
        SetQuantity(Quantity -= 1);
        SetText(GetQuantity().ToString());
        if (healthController == null)//Allow it to find health controller
        {
            healthController = GameObject.FindAnyObjectByType<CombatController>();
        }
        if (healthController != null)
        {
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
