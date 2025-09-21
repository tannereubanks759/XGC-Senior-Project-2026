using UnityEngine;
using UnityEngine.EventSystems;
public class inventorySlot : MonoBehaviour, IDropHandler
{
    private objectIdentifier objectIdentifier;
    private objectIdentifier objectIdentifierN;
    //public GameObject player;
    //public GameObject spawnPoint;
    public infoscript infoscriptRef;
    public equipScript equipS;
   
    private void Start()
    {
        infoscriptRef = FindAnyObjectByType<infoscript>();
        equipS = FindAnyObjectByType<equipScript>();
    }
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
            objectIdentifierN = dropped.GetComponent<objectIdentifier>();
            //objectIdentifierN.item.type
            if(objectIdentifierN.item.type == transform.tag)
            {
               
                DragScript dragScript = dropped.GetComponent<DragScript>();
                dragScript.parentAfterDrag = transform;
                equipS.equip(objectIdentifierN.item);
            }
           else
            {
                Debug.Log("Wrong slot.");
            }
        }
        /*else
        {
            GameObject dropped = eventData.pointerDrag;
            objectIdentifierN = dropped.GetComponent<objectIdentifier>();
            //objectIdentifierN.item.type
            if (objectIdentifierN.item.type == transform.tag)
            {

                DragScript dragScript = dropped.GetComponent<DragScript>();
                dragScript.parentAfterDrag = transform;
                equipS.equip(objectIdentifierN.item);
            }
        }*/
        
    }
    public void deleteAndDrop(GameObject obj)
    {
        objectIdentifier = obj.GetComponent<objectIdentifier>();
        objectIdentifier.item.OnUnEquip(infoscriptRef.player);
        GameObject item = Instantiate(objectIdentifier.item.prefab, new Vector3(17.8579998f, 25.382f, 236.531006f), Quaternion.identity);
        //item.transform.localPosition = Vector3.zero;
        //item.transform.localRotation = Quaternion.identity;
        //item.transform.localScale = Vector3.one;
        Debug.Log("Spawned");
        Destroy(obj);
        
    }
}
    

