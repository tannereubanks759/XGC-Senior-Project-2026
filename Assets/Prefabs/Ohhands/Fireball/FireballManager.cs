using UnityEngine;

public class FireballManager : MonoBehaviour
{
    public GameObject parent;
    public KeyCode useKey = KeyCode.F;
    public GameObject FireballPref;
    public GameObject fireball_1;
    public GameObject fireball_2;
    public int activeFireballs = 2;

    [Header("Charging / Scale")]
    public float totalChargeTime = 8f;                 // seconds from 0 → 100%
    [Range(0f, 1f)] public float scaleAtPopStart = 0.6f;       // 60% size before pop
    [Range(0f, 1f)] public float popStartTimeFraction = 0.75f; // pop in last 25% of time

    private Vector3 targetScale;

    private bool fireball_1_active = true;
    private bool fireball_2_active = true;

    // timers for the recharge of each fireball
    private float fireball1ChargeTimer = 0f;
    private float fireball2ChargeTimer = 0f;

    private bool upgradeOne = false; //"Increase Splash Range"
    private bool upgradeTwo = false; //"Set Enemies On Fire For A Few Seconds"

    void Start()
    {
        parent.SetActive(false);
        targetScale = fireball_1.transform.localScale; // assumed final/full scale
    }

    void Update()
    {
        // --- ONLY ONE CAN CHARGE AT A TIME, AND WE PRIORITIZE WHICHEVER STARTED FIRST ---

        bool fb1NeedsCharge = !fireball_1_active;
        bool fb2NeedsCharge = !fireball_2_active;

        if (fb1NeedsCharge && fb2NeedsCharge)
        {
            // Both are empty. Prioritize the one that has already started charging
            // (higher timer). If equal, fireball_1 wins the tie.
            if (fireball1ChargeTimer >= fireball2ChargeTimer)
            {
                RechargeFireball(fireball_1, ref fireball_1_active, ref fireball1ChargeTimer);
            }
            else
            {
                RechargeFireball(fireball_2, ref fireball_2_active, ref fireball2ChargeTimer);
            }
        }
        else if (fb1NeedsCharge)
        {
            RechargeFireball(fireball_1, ref fireball_1_active, ref fireball1ChargeTimer);
        }
        else if (fb2NeedsCharge)
        {
            RechargeFireball(fireball_2, ref fireball_2_active, ref fireball2ChargeTimer);
        }

        // ------------------------------------------------------------------------------

        if (Input.GetKeyDown(KeyCode.Alpha7)) //DEBUG KEY TO OPEN FIRE OBJECT
        {
            parent.SetActive(!parent.activeSelf);
        }

        if (!parent.activeSelf) return; //wont work if fireballs disabled past this point

        if (Input.GetKeyDown(useKey) && activeFireballs > 0)
        {
            Throw();
        }
    }

    /// <summary>
    /// Charges a fireball from 0 → 100% over totalChargeTime.
    /// - First part: grows up to ~60% linearly.
    /// - Last part: pops from 60% → 100% with a cubic (exponential-feeling) curve.
    /// </summary>
    void RechargeFireball(GameObject fb, ref bool isActive, ref float chargeTimer)
    {
        // we only call this when !isActive, so no need to early-return

        chargeTimer += Time.deltaTime;
        float t = Mathf.Clamp01(chargeTimer / totalChargeTime); // normalized 0–1

        float scale01;

        if (t <= popStartTimeFraction)
        {
            // linear 0 → scaleAtPopStart over the first portion of time
            float linT = t / popStartTimeFraction; // remap [0, popStartTimeFraction] to [0,1]
            scale01 = Mathf.Lerp(0f, scaleAtPopStart, linT);
        }
        else
        {
            // pop phase: scaleAtPopStart → 1 with a cubic ease (feels exponential)
            float u = (t - popStartTimeFraction) / (1f - popStartTimeFraction); // [0,1]
            float eased = u * u * u; // cubic curve
            scale01 = Mathf.Lerp(scaleAtPopStart, 1f, eased);
        }

        fb.transform.localScale = targetScale * scale01;

        // when fully charged, mark as active and bump activeFireballs once
        if (t >= 1f && !isActive)
        {
            fb.transform.localScale = targetScale;
            isActive = true;
            activeFireballs++;
        }
    }

    public void UpgradeOne()
    {
        upgradeOne = true;
    }

    public void UpgradeTwo()
    {
        upgradeTwo = true;
    }

    void Throw()
    {
        activeFireballs--;
        int random = Random.Range(0, 2);

        if (random == 0)
        {
            if (fireball_1_active)
            {
                fireball_1_active = false;
                ActivateThrow(fireball_1, ref fireball1ChargeTimer);
            }
            else
            {
                fireball_2_active = false;
                ActivateThrow(fireball_2, ref fireball2ChargeTimer);
            }
        }
        else
        {
            if (fireball_2_active)
            {
                fireball_2_active = false;
                ActivateThrow(fireball_2, ref fireball2ChargeTimer);
            }
            else
            {
                fireball_1_active = false;
                ActivateThrow(fireball_1, ref fireball1ChargeTimer);
            }
        }
    }

    void ActivateThrow(GameObject obj, ref float chargeTimer)
    {
        GameObject fireball = Instantiate(FireballPref, obj.transform.position, Camera.main.transform.rotation);
        Fireball fb = fireball.GetComponent<Fireball>();

        if (upgradeOne)
        {
            fb.splashRadius *= 1.5f;
        }
        if (upgradeTwo)
        {
            fb.setEnemiesOnFire = true;
        }

        Destroy(fireball, 5f);

        // reset for recharge
        obj.transform.localScale = Vector3.zero;
        chargeTimer = 0f;          // start counting 0 → 8s again for this orb
    }
}
