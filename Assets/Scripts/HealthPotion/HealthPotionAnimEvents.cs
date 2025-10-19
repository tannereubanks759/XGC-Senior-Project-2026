using UnityEngine;

public class HealthPotionAnimEvents : MonoBehaviour
{
    public HealthPotion p;
    public Animator anim;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anim = GetComponent<Animator>();
    }
    void heal()
    {
        p = GameObject.FindAnyObjectByType<HealthPotion>();
        if(p != null)
        {
            p.ConsumeHealthPotion();
        }
        
        anim.ResetTrigger("Drink");
    }
}
