using UnityEngine;
using UnityEngine.Animations;
using System.Collections.Generic;

public class FireLamp : MonoBehaviour
{
    FireSourceScript fire;
    public GameObject text;
    public bool playerInRange = false;
    public bool hasCollected = false;
    public KeyCode collectKey = KeyCode.E;

    private FireballManager fm;
    private GameObject fireballParent;

    public LookAtConstraint constraint;

    Transform currentSource;

    void Awake()
    {
        fire = GetComponentInChildren<FireSourceScript>(true);
        if (text != null) text.SetActive(false);

        // Include inactive children too
        constraint = GetComponentInChildren<LookAtConstraint>(true);

        if (constraint == null)
        {
            Debug.LogError($"FireLamp on '{name}' couldn't find a LookAtConstraint in children.", this);
        }
        else
        {
            constraint.constraintActive = false;
            ClearConstraintSources();
        }
    }

    void Update()
    {
        if (playerInRange && !hasCollected && Input.GetKeyDown(collectKey))
        {
            if (fire != null) fire.isCollected = true;
            hasCollected = true;          // (you had this wrong)
            if (text != null) text.SetActive(false);

            // Optional: stop looking after collecting
            ClearConstraintSources();
            if (constraint != null) constraint.constraintActive = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Make sure we have it even if something odd happened
        if (constraint == null)
            constraint = GetComponentInChildren<LookAtConstraint>(true);

        if (constraint == null) return; // prevents crash

        EnsureLookAtSource(other.transform);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (fire == null || fire.isCollected) return;

        if (fireballParent == null)
        {
            var offhand = other.GetComponentInChildren<offhandHandler>();
            if (offhand != null) fireballParent = offhand.fireBall;
        }

        if (fm == null)
            fm = other.GetComponentInChildren<FireballManager>();

        bool canCollect = fireballParent != null
                          && fireballParent.activeSelf
                          && fm != null
                          && !fm.bothReadyOrRecharging;

        playerInRange = canCollect;
        if (text != null) text.SetActive(canCollect);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        if (text != null) text.SetActive(false);

        if (other.transform == currentSource)
        {
            ClearConstraintSources();
            if (constraint != null) constraint.constraintActive = false;
        }
    }

    void EnsureLookAtSource(Transform target)
    {
        if (constraint == null) return;

        // Don’t spam duplicates
        if (currentSource == target && constraint.constraintActive) return;

        ClearConstraintSources();

        var source = new ConstraintSource { sourceTransform = target, weight = 1f };
        constraint.AddSource(source);
        currentSource = target;

        constraint.constraintActive = true;
    }

    void ClearConstraintSources()
    {
        if (constraint == null) return;
        constraint.SetSources(new List<ConstraintSource>());
        currentSource = null;
    }
}