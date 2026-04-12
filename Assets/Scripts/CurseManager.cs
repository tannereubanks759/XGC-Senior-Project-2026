using UnityEngine;

public class CurseManager : MonoBehaviour
{
    public static CurseManager Instance { get; private set; }

    private System.Action onExpire;
    private float expireTime;
    private bool active;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RegisterCurse(float duration, System.Action expireCallback)
    {
        expireTime = Time.time + duration;
        onExpire = expireCallback;
        active = true;
    }

    public void CancelTimer()
    {
        active = false;
        onExpire = null;
    }

    void Update()
    {
        if (!active) return;
        if (Time.time >= expireTime)
        {
            active = false;
            var callback = onExpire;
            onExpire = null;
            callback?.Invoke();
        }
    }
}