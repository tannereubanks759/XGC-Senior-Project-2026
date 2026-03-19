using System.Collections.Generic;
using UnityEngine;

public class CombatTracker : MonoBehaviour
{
    public static CombatTracker Instance;

    [Header("Combat Exit Delay")]
    public float exitCombatDelay = 6f;

    private HashSet<int> activeHostiles = new HashSet<int>();
    private float lastCombatTime = -999f;
    private bool inCombat = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        bool shouldBeInCombat = activeHostiles.Count > 0 || Time.time < lastCombatTime + exitCombatDelay;

        if (shouldBeInCombat != inCombat)
        {
            inCombat = shouldBeInCombat;

            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.SetState(inCombat
                    ? MusicManager.MusicState.Combat
                    : MusicManager.MusicState.Exploration);
            }
        }
    }

    public void RegisterHostile(Component enemy)
    {
        if (enemy == null) return;

        activeHostiles.Add(enemy.GetInstanceID());
        lastCombatTime = Time.time;
    }

    public void UnregisterHostile(Component enemy)
    {
        if (enemy == null) return;

        activeHostiles.Remove(enemy.GetInstanceID());

        if (activeHostiles.Count > 0)
            lastCombatTime = Time.time;
    }

    public void RefreshCombat()
    {
        lastCombatTime = Time.time;
    }
}