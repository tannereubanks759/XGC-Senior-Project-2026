using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class DragScript : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Transform parentAfterDrag;
    public Image image;
    public artifactStarter currentSlotChanged;
    private string nameOfArtifact;
    private Transform originalParent;
    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        parentAfterDrag = transform.parent;
        transform.SetParent(transform.root);
        transform.SetAsLastSibling();
        image.raycastTarget = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.SetParent(parentAfterDrag);       
        image.raycastTarget = true;

        currentSlotChanged = parentAfterDrag.GetComponent<artifactStarter>();
        if (currentSlotChanged != null && parentAfterDrag != originalParent) 
        {
            nameOfArtifact = image.sprite.name;
            currentSlotChanged.assignedArtifact(nameOfArtifact);
        }
    }


}
