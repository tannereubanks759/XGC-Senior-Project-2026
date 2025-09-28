using UnityEngine;
using UnityEngine.EventSystems;

public class tooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string nameOf;
    public string description;
    public void OnPointerEnter(PointerEventData eventData)
    {
        tooltipShower.Show(description, nameOf);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltipShower.Hide();
    }

    
}
