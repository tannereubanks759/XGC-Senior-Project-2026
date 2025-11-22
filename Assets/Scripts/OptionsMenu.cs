using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

public class OptionsMenu : MonoBehaviour
{
    public GameObject MainPauseScreen;
    public GameObject OptionsScreen;
    public Slider ambientSoundSlider;
    public Slider fxSoundSlider;
    public AudioMixer ambientAudioMixer;
    public AudioMixer fxAudioMixer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MainPauseScreen.SetActive(true);
        OptionsScreen.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenOptionsScreen()
    {
        MainPauseScreen.SetActive(false);
        OptionsScreen.SetActive(true);
    }
    public void BackButton()
    {
        MainPauseScreen.SetActive(true);
        OptionsScreen.SetActive(false);
    }
}
