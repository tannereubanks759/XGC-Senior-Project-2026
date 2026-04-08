using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class ButtonClickSound : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip clickSound;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (Button btn in FindObjectsOfType<Button>(true))
        {
            btn.onClick.AddListener(() => audioSource.PlayOneShot(clickSound));
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (Button btn in FindObjectsOfType<Button>(true))
        {
            btn.onClick.AddListener(() => audioSource.PlayOneShot(clickSound));
        }
    }
}
