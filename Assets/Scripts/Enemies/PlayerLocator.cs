using System;
using UnityEngine;

public class PlayerLocator : MonoBehaviour
{
    public static Transform PlayerRoot { get; private set; }
    public static event Action<Transform> OnPlayerReady;

    void Awake()
    {
        
        PlayerRoot = transform;
        OnPlayerReady?.Invoke(PlayerRoot);
    }

    void OnDestroy()
    {
        if (PlayerRoot == transform)
            PlayerRoot = null;
    }
}