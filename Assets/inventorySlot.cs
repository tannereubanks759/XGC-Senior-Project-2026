using UnityEngine;
using UnityEngine.EventSystems;
public class inventorySlot : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (transform.childCount == 0)
        {
            GameObject dropped = eventData.pointerDrag;
            DragScript dragScript = dropped.GetComponent<DragScript>();
            dragScript.parentAfterDrag = transform;
        }
        
    }

    
}
