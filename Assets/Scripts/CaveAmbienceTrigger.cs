using UnityEngine;

/// <summary>
/// Place on cave trigger colliders (isTrigger=true).
/// Notifies AmbientZoneBlender when player enters/exits cave.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class CaveAmbienceTrigger : MonoBehaviour
{
    private AmbientZoneBlender ambience;

    [Tooltip("Player tag to detect.")]
    public string playerTag = "Player";

    private void Reset()
    {
        Collider c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }
        ambience = other.GetComponentInChildren<AmbientZoneBlender>();
        
        if (ambience == null) return;
        ambience.EnterCave();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }
        ambience = other.GetComponentInChildren<AmbientZoneBlender>();

        if (ambience == null) return;
        ambience.ExitCave();
    }
}
