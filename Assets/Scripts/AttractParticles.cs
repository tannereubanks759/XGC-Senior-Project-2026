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
    private ParticleSystem.Particle[] particles;

    [Header("Coin Settings")]
    public int goldPerCoin = 10;           // Amount per particle
    public float spinSpeed = 1440f;         // Degrees/sec around Y axis

    [Header("Vacuum Settings")]
    public float delayBeforeVacuum = 0.6f; // Bounce time
    public float attractionStrength = 100f; // Force toward player
    public float maxSpeed = 18f;           // Smooth cap
    public float collectDistance = 10f;   // Auto-collect range

    private CollectionPool collPool;
    private Transform player;
    private float spawnTime;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        particles = new ParticleSystem.Particle[ps.main.maxParticles];

        player = GameObject.FindGameObjectWithTag("Player").transform;
        collPool = FindAnyObjectByType<CollectionPool>();

        spawnTime = Time.time;
    }

    Vector3 PlayerPos()
    {
        Vector3 playerPos = player.position;
        playerPos.y += 0.25f;

        return playerPos;
    }

    void LateUpdate()
    {
        int alive = ps.GetParticles(particles);

        Vector3 playerPos = PlayerPos();

        bool canVacuum = (Time.time - spawnTime) > delayBeforeVacuum;

        for (int i = 0; i < alive; i++)
        {
            ParticleSystem.Particle p = particles[i];

            // ---- SPIN THE COIN ----
            Quaternion rot = Quaternion.Euler(0, spinSpeed * Time.deltaTime, 0);
            p.rotation3D = rot * p.rotation3D;

            if (canVacuum)
            {
                Vector3 dir = (playerPos - p.position);
                float dist = dir.magnitude;

                dir.Normalize();

                // ---- VACUUM FORCE ----
                Vector3 vel = p.velocity + dir * (attractionStrength * Time.deltaTime);

                if (vel.magnitude > maxSpeed)
                    vel = vel.normalized * maxSpeed;

                p.velocity = vel;

                // ---- COLLECTION ----
                if (dist <= collectDistance)
                {
                    collPool.CollectGoldEffects(p.position);
                    player.GetComponent<GoldBank>().AddGold(goldPerCoin);
                    p.remainingLifetime = 0f;  // Kill particle
                }
            }

            particles[i] = p;
        }

        ps.SetParticles(particles, alive);
    }
}
