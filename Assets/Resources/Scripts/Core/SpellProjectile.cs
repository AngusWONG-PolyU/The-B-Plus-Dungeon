using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellProjectile : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 15f;
    public bool isStationary = false; // Set to true for AOE/Positional skills (Meteor)

    [Header("Life Cycle")]
    public bool manualDestruction = false;
    public bool waitForParticles = true; // If true, destroys after particle system finishes
    public float lifetime = 3f; // Fallback lifetime
    public bool destroyOnImpact = false;

    [Header("Combat")]
    public int damage = 1;
    public bool isHealing = false;
    public int healAmount = 1;

    [Header("Visuals")]
    public GameObject hitEffect;

    private string casterTag;
    private HashSet<GameObject> hitTargets = new HashSet<GameObject>();

    public void SetCaster(string tag)
    {
        casterTag = tag;
    }

    void Start()
    {
        if (manualDestruction) return;

        ParticleSystem ps = GetComponent<ParticleSystem>();
        
        if (waitForParticles && ps != null)
        {
            var main = ps.main;
            main.loop = false;

            // Calculate total duration (Duration + Max Particle Lifetime)
            float totalDuration = main.duration + main.startLifetime.constantMax;
            Destroy(gameObject, totalDuration);
        }
        else
        {
            // Fallback to fixed lifetime
            Destroy(gameObject, lifetime);
        }
    }

    void Update()
    {
        // Move the object forward if it's a projectile
        if (!isStationary)
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Projectile hit: {other.name} (Tag: {other.tag})");

        // 1. Ignore the Caster
        // Allow self-hit ONLY if it is a stationary healing spell
        if (casterTag != null && other.CompareTag(casterTag))
        {
            if (!(isHealing && isStationary)) return;
        }
        
        // 2. Ignore Trigger colliders
        if (other.isTrigger) return;

        // Prevent multiple hits on the same target
        if (hitTargets.Contains(other.gameObject)) return;
        hitTargets.Add(other.gameObject);

        // 3. Handle Healing
        if (isHealing)
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.Heal(healAmount);
            }
        }
        else
        {
            // 4. Handle Damage
            // If Player fired it -> Hit Enemy
            if (casterTag == "Player" && other.CompareTag("Enemy"))
            {
                EnemyController enemy = other.GetComponent<EnemyController>();
                if (enemy != null) enemy.TakeDamage(damage); // Basic damage
            }
            // If Enemy fired it -> Hit Player
            else if (casterTag == "Enemy" && other.CompareTag("Player"))
            {
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                }
                else
                {
                    Debug.Log("Player hit by Enemy Magic, but no PlayerHealth script found!");
                }
            }
        }

        if (destroyOnImpact)
        {
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }
            Destroy(gameObject);
        }
    }
}