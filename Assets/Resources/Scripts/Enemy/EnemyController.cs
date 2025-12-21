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
    
    [Header("References")]
    public Transform player;
    private DungeonRoomController roomController;
    
    private Animator animator;
    private bool isDead = false;
    private bool isAttacking = false;
    private int currentHealth;
    private SpriteRenderer spriteRenderer;
    private GameObject currentActiveMagic;
    private GameObject activeLockMagic;

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

        // Find room controller in parent
        roomController = GetComponentInParent<DungeonRoomController>();
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
            if (lockSkill != null) StartCoroutine(CastMagicRoutine(lockSkill));
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (attackSkill != null) StartCoroutine(CastMagicRoutine(attackSkill));
        }
        
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
        if (isAttacking || attackSkill == null) return;
        
        StartCoroutine(CastMagicRoutine(attackSkill));
    }

    IEnumerator CastMagicRoutine(SkillData skill)
    {
        isAttacking = true;
        
        if (animator != null)
        {
            animator.SetBool("isCasting", true);
        }
        
        // Wait for the cast duration from SkillData
        yield return new WaitForSeconds(skill.castTime);
        
        // Spawn the magic
        if (skill.skillPrefab != null && player != null)
        {
            GameObject newMagic = null;

            // If this is the Lock Skill
            if (skill == lockSkill)
            {
                CharacterMovement cm = player.GetComponent<CharacterMovement>();

                // Destroy previous lock magic if it exists
                if (activeLockMagic != null) 
                {
                    Destroy(activeLockMagic);
                    if (cm != null) cm.SetLocked(false);
                }
                
                activeLockMagic = Instantiate(skill.skillPrefab, player.position, Quaternion.identity);
                newMagic = activeLockMagic;
                
                // Lock the player
                if (cm != null)
                {
                    cm.StopMovement();
                    cm.SetLocked(true);
                }
            }
            else
            {
                // For other skills (Attack), destroy previous active magic (non-lock)
                if (currentActiveMagic != null) 
                {
                    Destroy(currentActiveMagic);
                }
                
                currentActiveMagic = Instantiate(skill.skillPrefab, player.position, Quaternion.identity);
                newMagic = currentActiveMagic;
            }
            
            // Initialize Projectile
            if (newMagic != null)
            {
                SpellProjectile proj = newMagic.GetComponent<SpellProjectile>();
                if (proj != null) 
                {
                    proj.SetCaster("Enemy");
                    
                    // If this is the Lock Skill, prevent auto-destruction and disable damage
                    if (skill == lockSkill)
                    {
                        proj.manualDestruction = true;
                        proj.damage = 0;
                    }
                }
            }
        }
        
        if (animator != null)
        {
            animator.SetBool("isCasting", false);
        }
        
        // Small cooldown
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

    public void ResetEnemy()
    {
        isDead = false;
        currentHealth = maxHealth;
        isAttacking = false;
        
        // Reset Animation
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
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
            roomController.EnemyDefeated(gameObject);
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
}