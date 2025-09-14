using UnityEngine;
using UnityEngine.EventSystems;
public class inventorySlot : MonoBehaviour, IDropHandler
{
    private objectIdentifier objectIdentifier;
    //public GameObject spawnPoint;
    public void OnDrop(PointerEventData eventData)
    {
        if(transform.CompareTag("UIDropArea"))
        {
            Debug.Log("Deleted item");
            GameObject dropped = eventData.pointerDrag;
            DragScript dragScript = dropped.GetComponent<DragScript>();
            dragScript.parentAfterDrag = transform;
            deleteAndDrop(dropped);
        }
        if (transform.childCount == 0)
        {
            GameObject dropped = eventData.pointerDrag;
            DragScript dragScript = dropped.GetComponent<DragScript>();
            dragScript.parentAfterDrag = transform;
        }
        
    }
    public void deleteAndDrop(GameObject obj)
    {
        objectIdentifier = obj.GetComponent<objectIdentifier>();
        Instantiate(objectIdentifier.item.prefab);
        Debug.Log("Spawned");
        
    }
}
    

