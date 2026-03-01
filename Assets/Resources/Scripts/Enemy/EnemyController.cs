using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour, ITaskTrigger
{
    private bool isTaskCompleted = false;

    [Header("Stats")]
    public Sprite enemyIcon;
    public bool isBoss = false;
    public int maxHealth = 3;
    public float detectionRange = 10f;
    
    [Header("Skills")]
    public SkillData attackSkill;
    public SkillData lockSkill;

    [Header("Combat Settings")]
    public float attackRange = 5f;
    public float attackInterval = 3f;
    public float chantDuration = 30f;

    private bool firstEncounter = true;
    private float attackTimer = 0f;
    private bool isLocked = false;
    private bool isChanting = false;
    private bool isFrozen = false;
    private bool criticalVulnerable = false;
    private bool forceFinishChant = false; 
    private Coroutine combatCoroutine;
    
    [Header("References")]
    public Transform player;
    public GameObject shieldObject;
    private DungeonRoomController roomController;
    
    private Animator animator;
    private bool isDead = false;
    private bool isAttacking = false;
    private int currentHealth;
    private GameObject currentActiveMagic;
    private GameObject activeLockMagic;
    private PlayerHealth playerHealth;

    private float baseChantDuration; // Store original duration

    private void Awake()
    {
        if (isBoss) maxHealth = 5;
        baseChantDuration = chantDuration; // Initialize base duration once
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
        
        // Initialize Shield logic
        UpdateShield(true);
        
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

        // Always face the player if within detection range and not frozen
        if (distance <= detectionRange && !isFrozen)
        {
            FacePlayer();
        }

        // Combat Logic
        if (distance <= attackRange)
        {
            if (firstEncounter)
            {
                firstEncounter = false;
                if (EnemyHealthBar.Instance != null)
                {
                    string displayName = isBoss ? "Boss" : "Enemy";
                    EnemyHealthBar.Instance.Setup(displayName, enemyIcon, currentHealth, maxHealth);
                }
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
            if (PlayerInstructionUI.Instance != null)
            {
                PlayerInstructionUI.Instance.HideInstruction();
            }
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
        // Inform player to Left Click
        if (PlayerInstructionUI.Instance != null)
        {
            PlayerInstructionUI.Instance.ShowInstruction("Left Click Anywhere to Unravel the Spell!");
        }

        isLocked = true; // Now waiting for player input
    }

    IEnumerator ChantRoutine()
    {
        isLocked = false; // Transitioning to chant
        isChanting = true;
        isAttacking = true;
        isFrozen = false;
        criticalVulnerable = false;
        isTaskCompleted = false;
        forceFinishChant = false;

        if (animator != null) animator.SetBool("isCasting", true);

        // Start the Task
        if (BPlusTreeTaskManager.Instance != null)
        {
            BPlusTreeTaskManager.Instance.StartTask(this, BPlusTreeTaskType.Deletion);
        }
        
        // Chant duration with check for player unlock
        float timer = 0f;
        while (timer < chantDuration)
        {
            if (forceFinishChant)
            {
                break;
            }

            // Update Task Timer UI
            if (BPlusTreeTaskManager.Instance != null)
            {
                BPlusTreeTaskManager.Instance.UpdateTaskTimer(chantDuration - timer, chantDuration);
            }

            // If the player completes the task successfully
            if (isTaskCompleted)
            {
                // Destroy the lock magic for the player
                Destroy(activeLockMagic);
                
                // Unlock player when magic is destroyed
                if (player != null)
                {
                    CharacterMovement cm = player.GetComponent<CharacterMovement>();
                    if (cm != null) cm.SetLocked(false);
                }
                
                isFrozen = true;
                UpdateShield(false); // Disable Shield
                if (timer < chantDuration / 2f)
                {
                    criticalVulnerable = true;
                    Debug.Log("Critical Vulnerability! Player unlocked before chant is finished halfway.");
                    if (PlayerInstructionUI.Instance != null)
                    {
                        PlayerInstructionUI.Instance.ShowInstruction("Speed Breaker! Rapid Mana Collapse Exposed a Fatal Flaw!\nNEXT ATTACK CRITICAL!", 3f);
                    }
                }
                else
                {
                    Debug.Log("Player unlocked before the chant finished. Freezing.");
                    if (PlayerInstructionUI.Instance != null)
                    {
                        PlayerInstructionUI.Instance.ShowInstruction("Spell Broken! Enemy Stunned by Backlash!\nATTACK NOW!", 3f);
                    }
                }

                // Trigger Freeze Animation State
                if (animator != null) animator.SetBool("isFrozen", true);
                
                // Wait indefinitely until interrupted (by attack)
                yield return new WaitUntil(() => !isFrozen);
                
                yield break;
            }

            yield return null;
            timer += Time.deltaTime;
        }
        
        // If we reached here, the timeout occurred (Task failed or time ran out)

        string failMessage = forceFinishChant ? "Disruption Failed! Energy Surge Accelerated the Spell!\nBRACE FOR IMPACT!" : "Time's Up! Spell Cast Complete!\nBRACE FOR IMPACT!";
        if (PlayerInstructionUI.Instance != null)
        {
            PlayerInstructionUI.Instance.ShowInstruction(failMessage, 3f, true);
        }

        if (BPlusTreeTaskManager.Instance != null)
        {
            BPlusTreeTaskManager.Instance.CloseTask(false);
        }

        // Cleanup lock magic on failure
        if (activeLockMagic != null)
        {
            Destroy(activeLockMagic);
            if (player != null)
            {
                CharacterMovement cm = player.GetComponent<CharacterMovement>();
                if (cm != null) cm.SetLocked(false);
            }
        }

        // If we reached here, the attack was not interrupted, and the player didn't unlock before the chant finished
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
        
        // Restore Shield if interrupted
        UpdateShield(true);

        if (animator != null) 
        {
            animator.SetBool("isCasting", false);
            animator.SetBool("isFrozen", false);
            animator.speed = 1f; 
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
        
        // Enemy only takes damage when Frozen
        if (!isFrozen) return;
        
        // Handle Critical State
        if (criticalVulnerable)
        {
            damage *= 2; // Critical Hit
            Debug.Log("Critical Hit applied!");
        }
        
        InterruptAttack();
        
        currentHealth -= damage;
        
        if (EnemyHealthBar.Instance != null)
        {
            EnemyHealthBar.Instance.UpdateHealthBar(currentHealth, maxHealth);
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

    public void SetChantDurationMultiplier(float multiplier)
    {
        chantDuration = baseChantDuration * multiplier;
    }

    public void ForceFinishChant()
    {
        forceFinishChant = true;
    }

    public void ResetEnemy()
    {
        isDead = false;
        currentHealth = maxHealth;
        
        UpdateShield(true); // Reset Shield
        
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
            animator.SetBool("isFrozen", false);
            animator.SetBool("isCasting", false);
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
        UpdateShield(false); // Remove Shield
        
        if (EnemyHealthBar.Instance != null)
        {
            EnemyHealthBar.Instance.Hide();
        }
        
        if (PlayerInstructionUI.Instance != null)
        {
            string defeatMessage = isBoss ? "Boss Defeated!" : "Enemy Defeated!";
            PlayerInstructionUI.Instance.ShowInstruction(defeatMessage, 3f);
        }

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

    // Shield Helper
    void UpdateShield(bool active)
    {
        if (shieldObject != null)
        {
            shieldObject.SetActive(active);
        }
    }

    void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDeath.RemoveListener(OnPlayerDied);
        }
    }

    // Call from Manager
    public void OnTaskComplete(bool success)
    {
        if (success)
        {
            isTaskCompleted = true;
        }
    }
}