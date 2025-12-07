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
            if (lockSkill != null) StartCoroutine(CastMagicRoutine(lockSkill));
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (attackSkill != null) StartCoroutine(CastMagicRoutine(attackSkill));
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
            // Destroy previous magic if it exists
            if (currentActiveMagic != null) 
            {
                Destroy(currentActiveMagic);
                CharacterMovement cm = player.GetComponent<CharacterMovement>();
                if (cm != null) cm.SetLocked(false);
            }
            
            currentActiveMagic = Instantiate(skill.skillPrefab, player.position, Quaternion.identity);
            
            // Initialize Projectile
            if (currentActiveMagic != null)
            {
                SpellProjectile proj = currentActiveMagic.GetComponent<SpellProjectile>();
                if (proj != null) proj.SetCaster("Enemy");
            }
            
            // If this is the Lock Skill, lock the player
            if (skill == lockSkill)
            {
                CharacterMovement cm = player.GetComponent<CharacterMovement>();
                if (cm != null)
                {
                    cm.StopMovement();
                    cm.SetLocked(true);
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