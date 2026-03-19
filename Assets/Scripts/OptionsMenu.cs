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

    void Start()
    {
        UpdateAmbientVolume();
        UpdateMusicVolume();
        UpdateFxVolume();
        UpdateMasterVolume();
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
        if (float.TryParse(input.text, out float sens))
        {
            controller.mouseSensitivity = sens;
        }
    }

    public void UpdateAmbientVolume()
    {
        float t = ambientSoundSlider.value;   // 0..1
        float dB = Mathf.Lerp(minVolumeDb, maxVolumeDb, t);
        ambientAudioMixer.SetFloat("AmbientVolume", dB);
    }

    public void UpdateMusicVolume()
    {
        float t = musicSoundSlider.value;   // 0..1
        float dB = Mathf.Lerp(minVolumeDb, maxVolumeDb, t);
        musicAudioMixer.SetFloat("MusicVolume", dB);
    }

    public void UpdateFxVolume()
    {
        float t = fxSoundSlider.value;   // 0..1
        float dB = Mathf.Lerp(minVolumeDb, maxVolumeDb, t);
        fxAudioMixer.SetFloat("FXVolume", dB);
    }
    public void UpdateMasterVolume()
    {
        float t = masterSlider.value;   // 0..1
        float dB = Mathf.Lerp(minMasterVolumeDb, maxMasterVolumeDb, t);
        masterAudioMixer.SetFloat("MasterVolume", dB);
    }
}