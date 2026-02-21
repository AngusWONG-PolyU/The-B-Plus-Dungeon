using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHearts = 3;
    public int currentHearts;

    [Header("Respawn Settings")]
    public Transform respawnPoint;
    public float respawnDelay = 3f;
    public GameObject respawnEffect;


    [Header("Events")]
    public UnityEvent<int> OnHealthChanged;
    public UnityEvent OnPlayerDeath;

    private bool isDead = false;
    private Animator animator;
    private CharacterMovement movement;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        movement = GetComponent<CharacterMovement>();

        currentHearts = maxHearts;

        if (respawnEffect != null)
            respawnEffect.SetActive(false);

        // Notify UI at start
        OnHealthChanged?.Invoke(currentHearts);
    }

    void Update()
    {
        // Debug: Press 'A' to take damage
        // if (Input.GetKeyDown(KeyCode.A))
        // {
        //     TakeDamage(1);
        // }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHearts -= damage;
        Debug.Log($"Player took {damage} damage. Current Hearts: {currentHearts}");

        if (animator != null) animator.SetTrigger("Hurt");

        if (currentHearts <= 0)
        {
            currentHearts = 0;
            Die();
        }

        OnHealthChanged?.Invoke(currentHearts);
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        currentHearts += amount;
        if (currentHearts > maxHearts)
        {
            currentHearts = maxHearts;
        }

        OnHealthChanged?.Invoke(currentHearts);
    }

    public void ResetHealth()
    {
        isDead = false;
        currentHearts = maxHearts;
        
        // Unlock movement
        if (movement != null) movement.ForceUnlock();
        
        // Re-enable Skills (Only if Dungeon is Active)
        PlayerSkillController skillController = GetComponent<PlayerSkillController>();
        if (skillController != null)
        {
            skillController.enabled = true;
            
            // Check Dungeon State
            DungeonManager dm = FindObjectOfType<DungeonManager>();
            if (dm != null)
            {
                skillController.isSystemActive = dm.isDungeonActive;
            }
        }
        
        // Reset Animation
        if (animator != null) animator.Play("Idle"); 

        Debug.Log("Player Health Reset to Max.");
        OnHealthChanged?.Invoke(currentHearts);
    }

    void Die()
    {
        isDead = true;
        Debug.Log("Player Died!");
        
        // Play Die Animation
        if (animator != null) animator.SetTrigger("Die");
        
        // Lock Movement
        if (movement != null) movement.SetLocked(true);

        // Disable Skills and Stop Casting
        PlayerSkillController skillController = GetComponent<PlayerSkillController>();
        if (skillController != null)
        {
            skillController.StopAllCoroutines(); // Stops any active cast
            skillController.enabled = false; // Prevents new inputs
        }

        OnPlayerDeath?.Invoke();
        
        // Start Respawn Sequence
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        // Wait for death animation
        yield return new WaitForSeconds(1f);

        // Play Respawn Effect
        if (respawnEffect != null)
        {
            respawnEffect.SetActive(true);
        }

        // Wait before teleport
        yield return new WaitForSeconds(1f);

        // Teleport to Respawn Point
        if (respawnPoint != null)
        {
            // Disable NavMeshAgent briefly to prevent snapping back
            UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            transform.position = respawnPoint.position;

            if (agent != null) agent.enabled = true;

            // Snap Camera
            IsometricCameraSetup cameraSetup = FindObjectOfType<IsometricCameraSetup>();
            if (cameraSetup != null)
            {
                cameraSetup.SnapToTarget();
            }
        }
        else
        {
            Debug.LogWarning("PlayerHealth: Respawn Point is not assigned!");
        }
        
        // Restore Health and Controls
        ResetHealth();

        // Wait after teleport
        yield return new WaitForSeconds(1f);

        // Disable Respawn Effect
        if (respawnEffect != null)
        {
            respawnEffect.SetActive(false);
        }
    }
}
