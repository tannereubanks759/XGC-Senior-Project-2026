using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class DragScript : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Transform parentAfterDrag;
    public Image image;
    public artifactStarter currentSlotChanged;
    private string nameOfArtifact;
    public Transform originalParent;
    private Canvas rootCanvas;
    private Transform dragLayer;
    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        dragLayer = rootCanvas.transform.Find("DragLayer");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        parentAfterDrag = transform.parent;
        transform.SetParent(rootCanvas.transform);
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

            //nameOfArtifact = image.sprite.name;
            //currentSlotChanged.assignedArtifact(nameOfArtifact);
            
        }
    }


}
