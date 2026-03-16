using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuperSlashProjectile : SpellProjectile
{
    [Header("Super Slash Settings")]
    public float piercingLifetime = 5f;
    private HashSet<GameObject> superHitTargets = new HashSet<GameObject>();

    protected override void Start()
    {
        // Inherit variables from SpellProjectile, ensure it pieces through everything
        destroyOnImpact = false; 
        
        // Destroy the slash after a set lifetime
        Destroy(gameObject, piercingLifetime);
    }

    protected override void OnTriggerEnter(Collider other)
    {
        // 1. Ignore the Caster (Player)
        if (other.CompareTag("Player")) return;
        
        // 2. Ignore regular Triggers
        if (other.isTrigger) return;

        // Prevent multiple hits on the same target
        if (superHitTargets.Contains(other.gameObject)) return;
        superHitTargets.Add(other.gameObject);

        bool hitSomething = false;

        // 3. One-Shot Kill Enemies (Bypassing Shield)
        if (other.CompareTag("Enemy"))
        {
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(enemy.maxHealth, true);
                hitSomething = true;
                Debug.Log($"[SuperSlash] Destroyed Enemy: {enemy.gameObject.name}");
                
                // Reward bypassing an enemy
                if (PerformanceManager.Instance != null) PerformanceManager.Instance.RecordSuperSlashBypass();
            }
        }
        
        // 4. Force Open/Destroy Doors
        DoorController door = other.GetComponent<DoorController>();
        if (door != null)
        {
            door.Open();
            hitSomething = true;
            Debug.Log($"[SuperSlash] Destroyed/Opened Door: {door.gameObject.name}");
            
            // Reward bypassing a door
            if (PerformanceManager.Instance != null) PerformanceManager.Instance.RecordSuperSlashBypass();
        }

        // 5. Play the hit effect without destroying the projectile
        if (hitSomething && hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, other.bounds.center, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }
}