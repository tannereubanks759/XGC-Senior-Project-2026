using NUnit.Framework;
using UnityEngine;

public class ChestScript : MonoBehaviour
{
    //public List<GameObject> artifactPool;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public interactScript interactScript;
    void Start()
    {
        
    }
    public void chestOpen()
    {
        if(interactScript.keyCount > 0) 
        { 
            interactScript.keyCount--;
            print("Chest opened");
            this.gameObject.SetActive(false);
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
