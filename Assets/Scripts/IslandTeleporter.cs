using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class IslandTeleporter : MonoBehaviour
{
    [Header("Teleporter Settings")]
    public string nextIsland;
    public bool isFinalTeleporter = false;
    public bool startOn = false;
    public AudioClip teleportSound;
    //Internals 
    private GameObject loadingScreen;
    private Animator doorAnim;
    private Slider slider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.GetComponent<Collider>().enabled = false;
        doorAnim = this.GetComponent<Animator>();
        if (startOn)
        {
            this.GetComponent<Collider>().enabled = true;
        }
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
            if (teleportSound)
            {
                this.GetComponent<AudioSource>().Stop();
                
                this.GetComponent<AudioSource>().PlayOneShot(teleportSound, .05f);
            }
            GameObject.FindAnyObjectByType<UImanager>().OpenWinScreen();
            Time.timeScale = 0f;
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
