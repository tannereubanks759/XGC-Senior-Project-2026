using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    public GameObject loadingScreen;
    public GameObject MainMenu;

    public Slider slider;

    public AudioSource buttonSound;
    private void Start()
    {
        loadingScreen.SetActive(false);
        MainMenu.SetActive(true);
    }
    public void LoadLevelBtn(string level)
    {
        if(buttonSound != null)
        {
            buttonSound.Play();
        }
        
        MainMenu.SetActive(false);
        loadingScreen.SetActive(true);
        StartCoroutine(LoadLevelAsync(level));
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
