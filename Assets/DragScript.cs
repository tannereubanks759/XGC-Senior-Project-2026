using System.Runtime.CompilerServices;
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
    public highlightScript[] allSlots;
    public GameObject[] slotObjs;
    public string artifactType;
    private objectIdentifier objIdent;
    
    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        dragLayer = rootCanvas.transform.Find("DragLayer");
        //allSlots = rootCanvas.GetComponentsInChildren<highlightScript>(true);
        objIdent = gameObject.GetComponent<objectIdentifier>(); 
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
       
        originalParent = transform.parent;
        parentAfterDrag = transform.parent;

        transform.SetParent(rootCanvas.transform);
        transform.SetAsLastSibling();
        image.raycastTarget = false;
        artifactType = objIdent.item.type.ToString();
        slotObjs = GameObject.FindGameObjectsWithTag("highlightSlot");
        allSlots = new highlightScript[slotObjs.Length];
        for (int i = 0; i < slotObjs.Length; i++)
        {
            allSlots[i] = slotObjs[i].GetComponent<highlightScript>();
        }
        foreach (var slot in allSlots)
        {
            if (slot != null)
            {
                slot.OnDragTypeSelected(artifactType);
            }
                
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.SetParent(parentAfterDrag);       
        image.raycastTarget = true;
        foreach (var slot in allSlots)
        {
            if (slot != null)
            {
                slot.OnDragEnded();
            }
                
        }
        currentSlotChanged = parentAfterDrag.GetComponent<artifactStarter>();
        if (currentSlotChanged != null && parentAfterDrag != originalParent) 
        {

            //nameOfArtifact = image.sprite.name;
            //currentSlotChanged.assignedArtifact(nameOfArtifact);
            
        }
    }


}
