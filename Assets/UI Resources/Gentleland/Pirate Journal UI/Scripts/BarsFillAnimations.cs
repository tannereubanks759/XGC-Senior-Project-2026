using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BarsFillAnimations : MonoBehaviour
{
    Slider[] sliders;
    float[] shifts;
    float[] fillTimeInSeconds;

    private void Start()
    {
        sliders = FindObjectsByType<Slider>(FindObjectsSortMode.None);
        shifts = new float[sliders.Length];
        fillTimeInSeconds = new float[sliders.Length];
        for (int i = 0; i < sliders.Length; i++)
        {
            fillTimeInSeconds[i] = Random.Range(1.0f, 4.0f);
            shifts[i] = Random.Range(0, fillTimeInSeconds[i]);
        }
    }

    void Update()
    {
        for(int i= 0;i<sliders.Length;i++)
        {
            float fill;
            float t = (shifts[i] + Time.realtimeSinceStartup) % (fillTimeInSeconds[i] * 2);
            if (t > fillTimeInSeconds[i])
            {
                fill = 1.0f - (t - fillTimeInSeconds[i]) / fillTimeInSeconds[i];
            }
            else
            {
                fill = t / fillTimeInSeconds[i];
            }
            sliders[i].SetValueWithoutNotify(fill);
        }
    }
}
