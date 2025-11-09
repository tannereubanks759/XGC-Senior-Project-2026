/*
 * AttractParticles.cs
 * 
 * This script controls a burst of particles that fly toward the player.
 * Each particle accelerates toward the player's position and disappears
 * once it gets close enough, adding gold to the player's total.
 * 
 * By: Matthew Bolger
 */
using System.Collections.Generic;
using UnityEngine;

public class AttractParticles : MonoBehaviour
{
    private ParticleSystem ps;
    private List<ParticleSystem.Particle> inside;
    private Transform player;

    [Header("Attraction Settings")]
    public float attractionStrength = 25f;    // Pull force per second
    public float maxSpeed = 15f;              // Smooth cap
    public float stopDistance = 1.5f;           // Collection range
    public float delayBeforeAttract = 3f;   // Let them bounce first

    [Header("Particle Setup")]
    public int goldCount = 1;

    private CollectionPool collPool;
    private float spawnTime;

    private void Start()
    {
        ps = GetComponent<ParticleSystem>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        collPool = GameObject.FindAnyObjectByType<CollectionPool>();

        inside = new List<ParticleSystem.Particle>();
        spawnTime = Time.time;

        // Setup emission
        var emission = ps.emission;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, goldCount)
        });

        // Setup trigger
        var triggers = ps.trigger;
        triggers.SetCollider(0, player.GetComponent<Collider>());
        triggers.inside = ParticleSystemOverlapAction.Callback;
    }

    void OnParticleTrigger()
    {
        // Skip attraction for a short moment after spawn (let initial velocity play)
        if (Time.time - spawnTime < delayBeforeAttract)
            return;

        int numInside = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, inside);
        Vector3 playerPos = player.position;

        for (int i = 0; i < numInside; i++)
        {
            ParticleSystem.Particle p = inside[i];

            Vector3 dir = (playerPos - p.position);
            float dist = dir.magnitude;
            dir.Normalize();

            // Accelerate toward player — keep consistent acceleration scale
            Vector3 velocity = p.velocity + dir * (attractionStrength * Time.deltaTime);

            // Clamp for smooth motion
            if (velocity.magnitude > maxSpeed)
                velocity = velocity.normalized * maxSpeed;

            p.velocity = velocity;
            
            // Collect when close enough
            if (dist <= stopDistance)
            {
                collPool.CollectGoldEffects(p.position);
                player.GetComponent<GoldBank>().AddGold(1);
                p.remainingLifetime = 0f;
            }

            inside[i] = p;
        }

        ps.SetTriggerParticles(ParticleSystemTriggerEventType.Inside, inside);
    }
}
