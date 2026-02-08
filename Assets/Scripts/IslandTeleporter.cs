using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class IslandTeleporter : MonoBehaviour
{
    [Header("Teleporter Settings")]
    public string nextIsland;
    public bool isFinalTeleporter = false;

    //Internals 
    private GameObject loadingScreen;
    private Animator doorAnim;
    private Slider slider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.GetComponent<Collider>().enabled = false;
        doorAnim = this.GetComponent<Animator>();
        
    }
    public void OpenDoor()
    {
        this.GetComponent<Collider>().enabled = true;
        doorAnim.SetBool("DoorOpen", true);
    }
    public void Teleport()
    {
        if (!isFinalTeleporter) //Go to next island
        {
            this.GetComponent<Collider>().enabled = false;
            loadingScreen = FindAnyObjectByType<FirstPersonController>().loadingScreen;
            slider = loadingScreen.GetComponentInChildren<Slider>();
            if (loadingScreen == null)
            {
                Debug.Log("Unable to find loading screen, aborting teleport");
                return;
            }
            if (slider == null)
            {
                Debug.Log("Unable to find loading slider, aborting teleport");
                return;
            }
            loadingScreen.SetActive(true);
            StartCoroutine(LoadLevelAsync(nextIsland));
        }
        else //beat game
        {
            GameObject.FindAnyObjectByType<UImanager>().OpenWinScreen();
        }
    }
    IEnumerator LoadLevelAsync(string level)
    {
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(level);

        while (!loadOperation.isDone)
        {
            float progressValue = Mathf.Clamp01(loadOperation.progress / .9f);
            slider.value = progressValue;
            yield return null;
        }
    }
}
