using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class offhandWheel : MonoBehaviour
{
    public UImanager UIM;
    private offhandHandler handler;
    private float currentPressTime;
    private float minHoldTime = 0.15f;
    private bool quickSwap = false;
    private bool wheelOpen = false;
    public AudioSource source;
    public AudioClip openingOffhandWheel;
    [Range(0f, 1f)] public float openVolume;
   // public AudioClip closingOffhandWheel;
   // [Range(0f, 1f)] public float closeVolume;
   // public AudioClip whileOpen;
    //[Range(0f, 1f)] public float whileOpenVolume;
    void Start()
    {
        handler = GameObject.FindAnyObjectByType<offhandHandler>();
    }
    private void PlaySound(AudioClip sound, float volume, bool isLooping)
    {
        if(source!=null && sound !=null) 
        {
            source.clip = sound;
            source.volume = volume;
            source.pitch = 0.5f;
            source.loop = isLooping;
            source.Play();
        }
    }
    private void StopSound()
    {
        if (source == null) return;
        source.loop = false;
        source.pitch = 1f;
        source.Stop();
    }
    public void openWheel()
    {
        if (wheelOpen) return;
        wheelOpen = true;
        PlaySound(openingOffhandWheel, openVolume, true);
        UIM.OpenOffhandWheelScreen();
        var controllerRef = FindAnyObjectByType<FirstPersonController>();
        controllerRef.playerCanMove = false;
        controllerRef.cameraCanMove = false;
        Volume vol = Camera.main.gameObject.GetComponent<Volume>();
        vol.enabled = true;
        Time.timeScale = .3f;
    }

    public void closeWheel()
    {
        if (!wheelOpen) return;
        wheelOpen = false;
        StopSound();
        var controllerRef = FindAnyObjectByType<FirstPersonController>();
        controllerRef.playerCanMove = true;
        controllerRef.cameraCanMove = true;
        var offhandHandle = FindAnyObjectByType<RMF_RadialMenu>();
        offhandHandle.buttonPress();
        Volume vol = Camera.main.gameObject.GetComponent<Volume>();
        vol.enabled = false;
        if (UIM != null && UIM.ModalOpen) return;
        UIM.OpenPlayerUIScreen();
    }

    void Update()
    {
        if (UIM != null && UIM.ModalOpen) return;
        if (Input.GetKeyDown(KeyCode.Q))
        {
            currentPressTime = Time.unscaledTime;
            quickSwap = false;
        }

        if (Input.GetKey(KeyCode.Q) && !quickSwap)
        {
            if (Time.unscaledTime - currentPressTime >= minHoldTime)
            {
                openWheel();
            }
        }

        if (Input.GetKeyUp(KeyCode.Q))
        {
            float pressDuration = Time.unscaledTime - currentPressTime;

            if (pressDuration < minHoldTime)
            {
                quickSwap = true;
                if (handler != null)
                {
                    handler.quickSwap();
                }
            }
            else
            {
                closeWheel();
            }
        }
    }
}