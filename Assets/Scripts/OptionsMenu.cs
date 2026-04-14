using TMPro;
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
    [Header("Master Volume Range (dB)")]
    public float minMasterVolumeDb = -40f;
    public float maxMasterVolumeDb = 10f;

    [Header("Ambience")]
    public Slider ambientSoundSlider;
    public AudioMixer ambientAudioMixer;

    [Header("Music")]
    public Slider musicSoundSlider;
    public AudioMixer musicAudioMixer;

    [Header("Fx")]
    public Slider fxSoundSlider;
    public AudioMixer fxAudioMixer;
    [Header("Master")]
    public Slider masterSlider;
    public AudioMixer masterAudioMixer;

    [Header("Sens")]
    public TMP_InputField input;
    public FirstPersonController controller;
    public float defaultSens = 2f;

    private static float ambientValue = 1000;
    private static float musicValue = 1000;
    private static float fxValue = 1000;
    private static float masterValue = 1000;
    void Start()
    {
        if(ambientValue == 1000f)
        {
            UpdateAmbientVolume();
            UpdateMusicVolume();
            UpdateFxVolume();
            UpdateMasterVolume();
        }
        SetSliderValues();
        if (input)
        {
            input.text = defaultSens.ToString();
            UpdateMouseSensitivity();
        }
        

        MainPauseScreen.SetActive(true);
        OptionsScreen.SetActive(false);
    }
    public void SetSliderValues()
    {
        ambientSoundSlider.value = ambientValue;
        fxSoundSlider.value = fxValue;
        masterSlider.value = masterValue;
        musicSoundSlider.value = musicValue;
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
        if (float.TryParse(input.text, out float sens))
        {
            controller.mouseSensitivity = sens;
        }
    }

    public void UpdateAmbientVolume()
    {
        ambientValue = ambientSoundSlider.value;   // 0..1
        float dB = Mathf.Lerp(minVolumeDb, maxVolumeDb, ambientValue);
        ambientAudioMixer.SetFloat("AmbientVolume", dB);
    }

    public void UpdateMusicVolume()
    {
        musicValue = musicSoundSlider.value;   // 0..1
        float dB = Mathf.Lerp(minVolumeDb, maxVolumeDb, musicValue);
        musicAudioMixer.SetFloat("MusicVolume", dB);
    }

    public void UpdateFxVolume()
    {
        fxValue = fxSoundSlider.value;   // 0..1
        float dB = Mathf.Lerp(minVolumeDb, maxVolumeDb, fxValue);
        fxAudioMixer.SetFloat("FXVolume", dB);
    }
    public void UpdateMasterVolume()
    {
        masterValue = masterSlider.value;   // 0..1
        float dB = Mathf.Lerp(minMasterVolumeDb, maxMasterVolumeDb, masterValue);
        masterAudioMixer.SetFloat("MasterVolume", dB);
    }
}