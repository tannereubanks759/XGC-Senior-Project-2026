using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;   


public class OptionsMenu : MonoBehaviour
{
    public GameObject MainPauseScreen;
    public GameObject OptionsScreen;

    [Header("Volume Range (dB)")]
    public float minVolumeDb = -40f;
    public float maxVolumeDb = 10f;


    [Header("Ambience")]
    public Slider ambientSoundSlider;
    public AudioMixer ambientAudioMixer;

    [Header("Fx")]
    public Slider fxSoundSlider;
    public AudioMixer fxAudioMixer;

    [Header("Sens")]
    public TMP_InputField input;
    public FirstPersonController controller;
    public float defaultSens = 2f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateAmbientVolume();
        UpdateFxVolume();
        input.text = defaultSens.ToString();
        UpdateMouseSensitivity();
        MainPauseScreen.SetActive(true);
        OptionsScreen.SetActive(false);
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

    public void UpdateMouseSensitivity()
    {
        controller.mouseSensitivity = float.Parse(input.text);
    }
    public void UpdateAmbientVolume()
    {
        float t = ambientSoundSlider.value;          // 0..1
        float dB = Mathf.Lerp(minVolumeDb, maxVolumeDb, t);
        ambientAudioMixer.SetFloat("MasterVolume", dB);
    }

    public void UpdateFxVolume()
    {
        float t = fxSoundSlider.value;              // 0..1
        float dB = Mathf.Lerp(minVolumeDb, maxVolumeDb, t);
        fxAudioMixer.SetFloat("MasterVolumeFX", dB);
    }

}
