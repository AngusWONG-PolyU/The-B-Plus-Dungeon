using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Stats")]
    public float attackRange = 5f;
    public float detectionRange = 10f;
    public int maxHealth = 3;
    public float castDuration = 2.0f; // Duration to cast before the magic appears
    
    [Header("References")]
    public Transform player;
    public GameObject lockMagicPrefab; // Prefab to lock the player
    public GameObject attackMagicPrefab; // Prefab to damage the player
    
    private Animator animator;
    private bool isDead = false;
    private bool isAttacking = false;
    private int currentHealth;
    private SpriteRenderer spriteRenderer;
    private GameObject currentActiveMagic;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
        
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
    }

    void Update()
    {
        // Test Input
        if (Input.GetKeyDown(KeyCode.T))
        {
            TakeDamage(1);
        }
        
        // Manual Cast Testing
        if (Input.GetKeyDown(KeyCode.L))
        {
            StartCoroutine(CastMagicRoutine(true)); // Cast Lock
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            StartCoroutine(CastMagicRoutine(false)); // Cast Attack
        }
        
        // Destroy Magic Test
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (currentActiveMagic != null) 
            {
                Destroy(currentActiveMagic);
                
                // Unlock player when magic is destroyed
                if (player != null)
                {
                    CharacterMovement cm = player.GetComponent<CharacterMovement>();
                    if (cm != null) cm.SetLocked(false);
                }
                
                Debug.Log("Magic destroyed by key press.");
            }
        }

        if (isDead || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Always face the player if within detection range
        if (distance <= detectionRange)
        {
            FacePlayer();
        }

        // Auto-attack logic disabled for manual testing
        /*
        if (distance <= attackRange)
        {
            Attack();
        }
        */
    }

    void FacePlayer()
    {
        // Calculate direction to player
        Vector3 direction = (player.position - transform.position).normalized;
        
        // Flip sprite based on direction relative to camera
        Vector3 localScale = transform.localScale;
        
        // Check dot product of direction vs camera right vector
        if (Camera.main != null)
        {
            Vector3 camRight = Camera.main.transform.right;
            float dot = Vector3.Dot(direction, camRight);
            
            if (dot > 0)
            {
                // Target is to the right
                if (localScale.x < 0) localScale.x = -localScale.x;
            }
            else if (dot < 0)
            {
                // Target is to the left
                if (localScale.x > 0) localScale.x = -localScale.x;
            }
            transform.localScale = localScale;
        }
    }

    void Attack()
    {
        if (isAttacking) return; // Don't start a new attack if already casting
        
        StartCoroutine(CastMagicRoutine(false));
    }

    IEnumerator CastMagicRoutine(bool isLockMagic)
    {
        isAttacking = true;
        
        if (animator != null)
        {
            // Start looping animation
            animator.SetBool("isCasting", true);
        }
        
        // Wait for the cast duration (animation loops during this time)
        yield return new WaitForSeconds(castDuration);
        
        // Choose magic based on parameter
        GameObject magicToSpawn = isLockMagic ? lockMagicPrefab : attackMagicPrefab;

        // Spawn the magic at the player's position
        if (magicToSpawn != null && player != null)
        {
            // Destroy previous magic if it exists
            if (currentActiveMagic != null) 
            {
                Destroy(currentActiveMagic);
                // If we are destroying old magic, make sure to unlock player just in case
                CharacterMovement cm = player.GetComponent<CharacterMovement>();
                if (cm != null) cm.SetLocked(false);
            }
            
            currentActiveMagic = Instantiate(magicToSpawn, player.position, Quaternion.identity);
            
            // If it is Lock Magic, lock the player
            if (isLockMagic)
            {
                CharacterMovement cm = player.GetComponent<CharacterMovement>();
                if (cm != null)
                {
                    // Stop movement first so they don't slide away from the magic
                    cm.StopMovement();
                    cm.SetLocked(true);
                }
            }
        }
        else
        {
            Debug.Log($"Magic Cast! Type: {(isLockMagic ? "Lock" : "Attack")}");
        }
        
        if (animator != null)
        {
            // Stop looping animation
            animator.SetBool("isCasting", false);
        }
        
        // Small cooldown before next cast
        yield return new WaitForSeconds(1f);
        
        isAttacking = false;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        
        if (currentHealth <= 0)
        {
            // Play hit animation first, then die
            if (animator != null) animator.SetTrigger("takeHit");
            StartCoroutine(DieAfterDelay(0.5f)); // Wait for hit animation
        }
        else
        {
            if (animator != null) animator.SetTrigger("takeHit");
        }
    }

    IEnumerator DieAfterDelay(float delay)
    {
        isDead = true;
        yield return new WaitForSeconds(delay);
        
        if (animator != null) animator.SetTrigger("die");
        
        // Disable physics
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        
        // Destroy after animation
        Destroy(gameObject, 1f);
    }
}