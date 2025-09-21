using UnityEngine;
using UnityEngine.EventSystems;
public class inventorySlot : MonoBehaviour, IDropHandler
{
    private objectIdentifier objectIdentifier;
    private objectIdentifier objectIdentifierN;
    private objectIdentifier objectIdentifierTwo;
    //public GameObject player;
    //public GameObject spawnPoint;
    public infoscript infoscriptRef;
    public equipScript equipS;
    public GameObject middleUIRef;
    private void Start()
    {
        infoscriptRef = FindAnyObjectByType<infoscript>();
        equipS = FindAnyObjectByType<equipScript>();
        middleUIRef = GameObject.FindGameObjectWithTag("middleUI");
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
            return;
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
                return;
            }
        }
        else
        {

            GameObject dropped = eventData.pointerDrag;
            objectIdentifierN = dropped.GetComponent<objectIdentifier>();
            DragScript dragScriptOne = dropped.GetComponent<DragScript>();
            //objectIdentifierN.item.type
            if (objectIdentifierN.item.type == transform.tag)
            {
                if(dragScriptOne.originalParent.CompareTag("middleUI")) 
                { 
                GameObject current = transform.GetChild(0).gameObject;
                objectIdentifierTwo = current.GetComponent<objectIdentifier>();
                current.transform.SetParent(middleUIRef.transform);
                DragScript dragScript = dropped.GetComponent<DragScript>();
                dragScript.parentAfterDrag = transform;
                objectIdentifierTwo.item.OnUnEquip(infoscriptRef.player);
                equipS.equip(objectIdentifierN.item);
                }
                else
                {
                    Debug.Log("SIDE SWAP STOPPED");
                    
                    return;
                }
                
            }
        }
        
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
    

