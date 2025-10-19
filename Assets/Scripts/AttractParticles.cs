using System.Collections.Generic;
using UnityEngine;

public class AttractParticles : MonoBehaviour
{
    private ParticleSystem ps;
    private List<ParticleSystem.Particle> inside;
    private Transform player;

    [Header("Attraction Settings")]
    public float attractionStrength = 1000f;   // How strongly they’re pulled in
    public float maxSpeed = 15f;             // Limit so it looks smooth
    public float stopDistance = 1f;       // When to consider “collected”

    public int goldCount;

    private void Start()
    {
        if (ps == null) ps = GetComponent<ParticleSystem>();

        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;

        inside = new List<ParticleSystem.Particle>();

        var emission = ps.emission;

        // Create a single burst at time 0, emitting goldCount particles
        var burst = new ParticleSystem.Burst(0f, (float)goldCount);

        // Assign it to the emission module
        emission.SetBurst(0, burst);

        var triggers = ps.trigger;

        triggers.SetCollider(0, player.GetComponent<Collider>());
    }


    void OnParticleTrigger()
    {
        // Get all particles currently inside the trigger
        int numInside = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, inside);

        Vector3 playerPos = player.position;
        Debug.Log(player.position);

        for (int i = 0; i < numInside; i++)
        {
            ParticleSystem.Particle p = inside[i];

            // Direction toward player
            Vector3 dir = (playerPos - p.position);
            float dist = dir.magnitude;
            dir.Normalize();

            // Accelerate particle toward player
            Vector3 velocity = p.velocity.normalized + dir * (attractionStrength * Time.deltaTime);

            // Clamp to max speed for smooth motion
            if (velocity.magnitude > maxSpeed)
                velocity = velocity.normalized * maxSpeed;

            p.velocity = velocity;

            // Optional: snap or remove if it gets very close
            if (dist < stopDistance)
            {
                p.remainingLifetime = 0; // kills particle (collected)
                // Trigger gold gain
                player.GetComponent<GoldBank>().AddGold(1);
            }

            inside[i] = p;
        }

        ps.SetTriggerParticles(ParticleSystemTriggerEventType.Inside, inside);
    }
}
