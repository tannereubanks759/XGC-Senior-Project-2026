using UnityEngine;
using UnityEngine.EventSystems;

public class tooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string nameOf;
    public string description;
    public string price;
    public void OnPointerEnter(PointerEventData eventData)
    {
        
        tooltipShower.Show(description, nameOf, price);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        
        tooltipShower.Hide();
    }

    
}
