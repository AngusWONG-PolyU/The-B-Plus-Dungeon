using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 3;
    public float detectionRange = 10f;
    
    [Header("Skills")]
    public SkillData attackSkill;
    public SkillData lockSkill;

    [Header("Combat Settings")]
    public float attackRange = 5f;
    public float attackInterval = 3f;
    public float chantDuration = 5f;

    private bool firstEncounter = true;
    private float attackTimer = 0f;
    private bool isLocked = false;
    private bool isChanting = false;
    private bool isFrozen = false;
    private bool criticalVulnerable = false;
    private Coroutine combatCoroutine;
    
    [Header("References")]
    public Transform player;
    public EnemyHealthBar healthBar;
    private DungeonRoomController roomController;
    
    private Animator animator;
    private bool isDead = false;
    private bool isAttacking = false;
    private int currentHealth;
    private SpriteRenderer spriteRenderer;
    private GameObject currentActiveMagic;
    private GameObject activeLockMagic;
    private PlayerHealth playerHealth;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
        
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
        
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.OnPlayerDeath.AddListener(OnPlayerDied);
            }
        }

        // Find room controller in parent
        roomController = GetComponentInParent<DungeonRoomController>();
    }

    void Update()
    {
        /*
        // Test Input
        if (Input.GetKeyDown(KeyCode.T))
        {
            TakeDamage(1);
        }
        */
        
        /*
        // Manual Cast Testing
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (lockSkill != null) StartCoroutine(CastMagicRoutine(lockSkill));
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (attackSkill != null) StartCoroutine(CastMagicRoutine(attackSkill));
        }
        */
        
        // Destroy Magic Test
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (activeLockMagic != null) 
            {
                Destroy(activeLockMagic);
                
                // Unlock player when magic is destroyed
                if (player != null)
                {
                    CharacterMovement cm = player.GetComponent<CharacterMovement>();
                    if (cm != null) cm.SetLocked(false);
                }
                
                Debug.Log("Lock Magic destroyed by key press.");
            }
            
            if (currentActiveMagic != null)
            {
                Destroy(currentActiveMagic);
            }
        }

        if (isDead || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Always face the player if within detection range
        if (distance <= detectionRange)
        {
            FacePlayer();
        }

        // Combat Logic
        if (distance <= attackRange)
        {
            if (firstEncounter)
            {
                firstEncounter = false;
                StartLockSequence();
            }
            else if (!isAttacking && !isLocked && !isChanting)
            {
                attackTimer += Time.deltaTime;
                if (attackTimer >= attackInterval)
                {
                    attackTimer = 0f;
                    StartLockSequence();
                }
            }
        }

        // Handle Click when Locked to start Chant
        if (isLocked && Input.GetMouseButtonDown(0))
        {
            StartChantSequence();
        }
    }

    void StartLockSequence()
    {
        if (combatCoroutine != null) StopCoroutine(combatCoroutine);
        combatCoroutine = StartCoroutine(LockRoutine());
    }

    void StartChantSequence()
    {
        if (combatCoroutine != null) StopCoroutine(combatCoroutine);
        combatCoroutine = StartCoroutine(ChantRoutine());
    }

    IEnumerator LockRoutine()
    {
        isAttacking = true;
        
        if (animator != null) animator.SetBool("isCasting", true);
        
        // Wait for cast time
        yield return new WaitForSeconds(lockSkill.castTime);
        
        // Spawn Lock Magic
        if (lockSkill.skillPrefab != null && player != null)
        {
            // Destroy previous lock magic if it exists
            if (activeLockMagic != null) Destroy(activeLockMagic);
            
            activeLockMagic = Instantiate(lockSkill.skillPrefab, player.position, Quaternion.identity);
            
            CharacterMovement cm = player.GetComponent<CharacterMovement>();
            if (cm != null)
            {
                cm.StopMovement();
                cm.SetLocked(true);
            }
            
            // Configure projectile
            SpellProjectile proj = activeLockMagic.GetComponent<SpellProjectile>();
            if (proj != null)
            {
                proj.SetCaster("Enemy");
                proj.manualDestruction = true;
                proj.damage = 0;
            }
        }
        
        if (animator != null) animator.SetBool("isCasting", false);
        
        isAttacking = false;
        isLocked = true; // Now waiting for player input
    }

    IEnumerator ChantRoutine()
    {
        isLocked = false; // Transitioning to chant
        isChanting = true;
        isAttacking = true;
        isFrozen = false;
        criticalVulnerable = false;
        
        if (animator != null) animator.SetBool("isCasting", true);
        
        // Chant duration with check for player unlock
        float timer = 0f;
        while (timer < chantDuration)
        {
            // If player unlocks themselves (magic destroyed), freeze enemy
            if (activeLockMagic == null)
            {
                isFrozen = true;
                if (timer < chantDuration / 2f)
                {
                    criticalVulnerable = true;
                    Debug.Log("Critical Vulnerability! Player unlocked before chant is finished halfway.");
                }
                else
                {
                    Debug.Log("Player unlocked before the chant finished. Freezing.");
                }

                // Pause Animation
                if (animator != null) animator.speed = 0f;
                
                // Freeze until attacked (wait indefinitely until interrupted)
                while (true)
                {
                    yield return null;
                }
            }

            yield return null;
            timer += Time.deltaTime;
        }
        
        // If we reached here, the attack was not interrupted and player didn't unlock before the chant finished
        if (attackSkill.skillPrefab != null && player != null)
        {
            if (currentActiveMagic != null) Destroy(currentActiveMagic);
            
            currentActiveMagic = Instantiate(attackSkill.skillPrefab, player.position, Quaternion.identity);
            
            SpellProjectile proj = currentActiveMagic.GetComponent<SpellProjectile>();
            if (proj != null)
            {
                proj.SetCaster("Enemy");
            }
        }
        
        // Cleanup
        UnlockPlayer();
        
        if (animator != null) animator.SetBool("isCasting", false);
        
        isChanting = false;
        isAttacking = false;
        
        // Small cooldown
        yield return new WaitForSeconds(1f);
    }

    void UnlockPlayer()
    {
        if (activeLockMagic != null) Destroy(activeLockMagic);
        
        if (player != null)
        {
            CharacterMovement cm = player.GetComponent<CharacterMovement>();
            if (cm != null) cm.SetLocked(false);
        }
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

    void InterruptAttack()
    {
        if (combatCoroutine != null) StopCoroutine(combatCoroutine);
        
        if (animator != null) 
        {
            animator.SetBool("isCasting", false);
            animator.speed = 1f; // Restore speed just in case
        }
        
        isChanting = false;
        isAttacking = false;
        isLocked = false;
        isFrozen = false;
        criticalVulnerable = false;
        
        Debug.Log("Enemy Attack Interrupted!");
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        
        // Handle Frozen/Critical State
        if (isFrozen)
        {
             if (criticalVulnerable)
             {
                 damage *= 2; // Critical Hit
                 Debug.Log("Critical Hit applied!");
             }
             
             // Unfreeze
             isFrozen = false;
             criticalVulnerable = false;
             if (animator != null) animator.speed = 1f;
        }
        
        if (isChanting || isFrozen) // isFrozen implies we were chanting/waiting
        {
            InterruptAttack();
        }
        
        currentHealth -= damage;
        
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
        
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

    public void ResetEnemy()
    {
        isDead = false;
        currentHealth = maxHealth;
        
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
        
        isAttacking = false;
        firstEncounter = true;
        attackTimer = 0f;
        isLocked = false;
        isChanting = false;
        isFrozen = false;
        criticalVulnerable = false;

        if (combatCoroutine != null) StopCoroutine(combatCoroutine);
        
        // Reset Animation
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
            animator.speed = 1f;
        }
        
        // Enable physics
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
        
        // Ensure active
        gameObject.SetActive(true);
    }

    IEnumerator DieAfterDelay(float delay)
    {
        isDead = true;
        
        // Unlock player and destroy magic if active
        if (activeLockMagic != null)
        {
            Destroy(activeLockMagic);
            if (player != null)
            {
                CharacterMovement cm = player.GetComponent<CharacterMovement>();
                if (cm != null) cm.SetLocked(false);
            }
        }

        if (currentActiveMagic != null)
        {
            Destroy(currentActiveMagic);
        }
        
        // Notify room controller
        if (roomController != null)
        {
            roomController.EnemyDefeated();
        }

        yield return new WaitForSeconds(delay);
        
        if (animator != null) animator.SetTrigger("die");
        
        // Disable physics
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        
        // Disable instead of Destroy to allow reuse
        yield return new WaitForSeconds(1f);
        gameObject.SetActive(false);
    }

    void OnPlayerDied()
    {
        if (activeLockMagic != null)
        {
            Destroy(activeLockMagic);
        }
    }

    void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDeath.RemoveListener(OnPlayerDied);
        }
    }
}