using System.Collections.Generic;
using UnityEngine;

public class TutorialScript : MonoBehaviour
{
    private int tutorialStep = 0;
    public TutorialMessage message;
    private float stepTimer = 0f;
    public static bool tutorialDone = false;
    public GameObject tutorialBackground;
    public WeaponsManager weaponsManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (tutorialDone)
        {
            tutorialBackground.SetActive(false);
            return;
        }
        ShowStep(0);
    }
    private void ShowStep(int step)
    {
        tutorialStep = step;
        if (step == 0) message.Show("Hold Q to open your offhand wheel.");
        if (step == 1) message.Show("Swap to your gun by pressing 2, or by using the scroll wheel.");
        if (step == 2) message.Show("Block by holding the right mouse button with your sword out.");
        if (step == 3) message.Show("While holding block, press the left mouse button to push enemies.");
        if (step == 4) message.Show("While still holding block, press space to dodge.");
        if (step == 5) message.Show("After taking damage and having enough potions, you can heal by pressing the H key.");
    }
    // Update is called once per frame
    void Update()
    {
        if (tutorialStep == 0 && Input.GetKeyUp(KeyCode.Q))
        {
            nextStep();
        }
        if (tutorialStep == 1 && (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetAxis("Mouse ScrollWheel") != 0f))
        {
            nextStep();
        }
        if (tutorialStep == 2 && weaponsManager.currentWeapon == 0 && Input.GetMouseButton(1))
        {
            nextStep();
        }
        if (tutorialStep == 3 && weaponsManager.currentWeapon == 0 && Input.GetMouseButton(1) && Input.GetMouseButtonDown(0))
        {
            nextStep();
        }
        if (tutorialStep == 4 && weaponsManager.currentWeapon == 0 && Input.GetMouseButton(1) && Input.GetKeyDown(KeyCode.Space))
        {
            nextStep();
        }
        if (tutorialStep == 5)
        {
            stepTimer += Time.deltaTime;
            if (stepTimer >= 7f)
            {
                stepTimer = 0f;
                nextStep();
            }
        }
    }
    private void nextStep()
    {
        message.Clear();
        tutorialStep++;
        if (tutorialStep > 5)
        {
            tutorialDone = true;
            tutorialBackground.SetActive(false);
            return;
        }
        ShowStep(tutorialStep);
    }
}
