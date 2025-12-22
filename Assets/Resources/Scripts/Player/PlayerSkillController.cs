using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSkillController : MonoBehaviour
{
    [Header("Skill Slots (Drag SkillData here)")]
    public SkillData[] equippedSkills = new SkillData[4]; // Corresponds to keys 1, 2, 3, 4

    [Header("References")]
    public CharacterMovement movementScript;
    public LayerMask groundLayer; // LayerMask for Raycasting
    
    [Header("Aiming Indicators")]
    public Transform directionalIndicator; // Shows direction
    public Transform positionalIndicator; // Shows position

    [Header("System Settings")]
    public bool isSystemActive = true;

    // Runtime State
    private int currentSkillIndex = -1;
    private bool isAiming = false;
    private float[] currentCooldowns;
    private Camera mainCamera;
    private Animator animator;

    // Public Getters for UI
    public float GetCurrentCooldown(int index)
    {
        if (currentCooldowns != null && index >= 0 && index < currentCooldowns.Length)
            return currentCooldowns[index];
        return 0f;
    }

    public int GetCurrentSkillIndex()
    {
        return isAiming ? currentSkillIndex : -1;
    }

    void Start()
    {
        mainCamera = Camera.main;
        if (movementScript == null) movementScript = GetComponent<CharacterMovement>();
        
        animator = GetComponentInChildren<Animator>();

        // Initialize Cooldown array
        currentCooldowns = new float[equippedSkills.Length];
        
        // Hide all Indicators
        HideIndicators();
    }

    void Update()
    {
        HandleCooldowns();
        HandleInput();
        
        if (isAiming)
        {
            UpdateAimingVisuals();
        }
    }

    void OnDisable()
    {
        // Ensure indicators are hidden when the script is disabled
        HideIndicators();
        isAiming = false;
    }

    void HandleCooldowns()
    {
        for (int i = 0; i < currentCooldowns.Length; i++)
        {
            if (currentCooldowns[i] > 0)
            {
                currentCooldowns[i] -= Time.deltaTime;
            }
        }
    }

    void HandleInput()
    {
        if (!isSystemActive)
        {
            if (isAiming) CancelAiming();
            return;
        }

        // 1. Skill Selection (Press 1-4)
        if (Input.GetKeyDown(KeyCode.Alpha1)) TryStartAiming(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) TryStartAiming(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) TryStartAiming(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) TryStartAiming(3);

        // 2. Cancel (Right Click)
        if (isAiming && (Input.GetMouseButtonDown(1)))
        {
            CancelAiming();
        }

        // 3. Cast (Left Click)
        if (isAiming && Input.GetMouseButtonDown(0))
        {
            // Ensure not clicking UI
            CastCurrentSkill();
        }
    }

    void TryStartAiming(int index)
    {
        // Check if slot has a skill equipped
        if (index < 0 || index >= equippedSkills.Length || equippedSkills[index] == null)
        {
            Debug.Log($"Slot {index + 1} is empty.");
            return;
        }
        
        // Check Cooldown
        if (currentCooldowns[index] > 0)
        {
            Debug.Log($"{equippedSkills[index].skillName} is on cooldown ({currentCooldowns[index]:F1}s).");
            return;
        }

        // If the same key is pressed again, cancel
        if (isAiming && currentSkillIndex == index)
        {
            CancelAiming();
            return;
        }

        // Start Aiming
        currentSkillIndex = index;
        isAiming = true;
        ShowIndicator(equippedSkills[index]);
        
        Debug.Log($"Aiming: {equippedSkills[index].skillName}");
    }

    void CancelAiming()
    {
        isAiming = false;
        currentSkillIndex = -1;
        HideIndicators();
    }

    void ShowIndicator(SkillData skill)
    {
        HideIndicators(); // Reset

        if (skill.aimType == SkillAimType.Directional)
        {
            if (directionalIndicator) directionalIndicator.gameObject.SetActive(true);
        }
        else if (skill.aimType == SkillAimType.Positional || skill.aimType == SkillAimType.Self)
        {
            if (positionalIndicator) positionalIndicator.gameObject.SetActive(true);
        }
    }

    void HideIndicators()
    {
        if (directionalIndicator) directionalIndicator.gameObject.SetActive(false);
        if (positionalIndicator) positionalIndicator.gameObject.SetActive(false);
    }

    void UpdateAimingVisuals()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        // Use LayerMask to only hit Ground
        // If groundLayer is not set, default to Everything
        int layerMask = groundLayer.value != 0 ? groundLayer.value : Physics.DefaultRaycastLayers;

        // Use RaycastAll to penetrate the player if they are in the way
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, layerMask);
        
        RaycastHit hit = new RaycastHit();
        bool foundHit = false;
        float closestDist = Mathf.Infinity;

        foreach (var h in hits)
        {
            // Ignore the player and any child colliders
            if (h.collider.transform.IsChildOf(transform)) continue;
            
            // Ignore triggers
            if (h.collider.isTrigger) continue;

            if (h.distance < closestDist)
            {
                closestDist = h.distance;
                hit = h;
                foundHit = true;
            }
        }

        if (foundHit)
        {
            Vector3 mousePos = hit.point;
            Vector3 playerPos = transform.position;
            
            // Ensure consistent height for calculation
            Vector3 flatMousePos = new Vector3(mousePos.x, playerPos.y, mousePos.z);
            
            SkillData currentSkill = equippedSkills[currentSkillIndex];

            if (currentSkill.aimType == SkillAimType.Directional)
            {
                if (directionalIndicator)
                {
                    // 1. Position: At player's feet (slightly raised)
                    directionalIndicator.position = playerPos + Vector3.up * 0.1f;

                    // 2. Rotation: Face the mouse
                    Vector3 lookTarget = flatMousePos;
                    lookTarget.y = directionalIndicator.position.y; // Ensure horizontal rotation
                    
                    if (Vector3.Distance(lookTarget, directionalIndicator.position) > 0.01f)
                    {
                        directionalIndicator.LookAt(lookTarget);
                    }

                    // 3. Scale/Size: Scale the whole indicator based on the 9-Sliced child's height
                    Transform slicedChild = directionalIndicator.Find("9-Sliced");
                    if (slicedChild != null)
                    {
                        SpriteRenderer sr = slicedChild.GetComponent<SpriteRenderer>();
                        if (sr != null && sr.size.y > 0)
                        {
                            // Calculate scale factor: Target Range / Base Sprite Height
                            float scaleFactor = currentSkill.range / sr.size.y;
                            
                            // Apply to Z axis (Forward direction)
                            Vector3 newScale = directionalIndicator.localScale;
                            newScale.z = scaleFactor;
                            directionalIndicator.localScale = newScale;
                        }
                    }
                }
            }
            else if (currentSkill.aimType == SkillAimType.Positional)
            {
                if (positionalIndicator)
                {
                    // 1. Position: Follow the mouse, clamped by range
                    float dist = Vector3.Distance(playerPos, flatMousePos);
                    Vector3 targetPos = flatMousePos;
                    
                    if (dist > currentSkill.range)
                    {
                        Vector3 dir = (flatMousePos - playerPos).normalized;
                        targetPos = playerPos + dir * currentSkill.range;
                    }
                    positionalIndicator.position = targetPos + Vector3.up * 0.1f;

                    // 2. Scale: Scan all child Particle Systems for the largest size
                    float maxDiameter = 1.0f; // Default fallback
                    
                    if (currentSkill.skillPrefab != null)
                    {
                        // Look in the children because the root might be a container
                        ParticleSystem[] particles = currentSkill.skillPrefab.GetComponentsInChildren<ParticleSystem>();
                        float largestSize = 0f;
                        
                        foreach (ParticleSystem ps in particles)
                        {
                            float currentSize = 0f;
                            var shape = ps.shape;
                            
                            if (shape.enabled)
                            {
                                // Use Radius * 2 for circular shapes
                                currentSize = shape.radius * 2f;
                                
                                // If it's a Box, use the largest horizontal scale
                                if (shape.shapeType == ParticleSystemShapeType.Box || shape.shapeType == ParticleSystemShapeType.Rectangle)
                                {
                                    currentSize = Mathf.Max(shape.scale.x, shape.scale.y);
                                }
                            }
                            else
                            {
                                // Fallback to start size if no shape is defined
                                currentSize = ps.main.startSize.constantMax;
                            }
                            
                            if (currentSize > largestSize) largestSize = currentSize;
                        }
                        
                        if (largestSize > 0) maxDiameter = largestSize;
                    }
                    
                    positionalIndicator.localScale = new Vector3(maxDiameter, maxDiameter, 1f);
                }
            }
            else if (currentSkill.aimType == SkillAimType.Self)
            {
                if (positionalIndicator)
                {
                    // 1. Position: Always at player's feet
                    positionalIndicator.position = transform.position + Vector3.up * 0.1f;

                    // 2. Scale: Same logic as Positional
                    float maxDiameter = 1.0f;
                    if (currentSkill.skillPrefab != null)
                    {
                        ParticleSystem[] particles = currentSkill.skillPrefab.GetComponentsInChildren<ParticleSystem>();
                        float largestSize = 0f;
                        foreach (ParticleSystem ps in particles)
                        {
                            float currentSize = ps.main.startSize.constantMax;
                            var shape = ps.shape;
                            if (shape.enabled)
                            {
                                currentSize = shape.radius * 2f;
                                if (shape.shapeType == ParticleSystemShapeType.Box || shape.shapeType == ParticleSystemShapeType.Rectangle)
                                {
                                    currentSize = Mathf.Max(shape.scale.x, shape.scale.y);
                                }
                            }
                            if (currentSize > largestSize) largestSize = currentSize;
                        }
                        if (largestSize > 0) maxDiameter = largestSize;
                    }
                    positionalIndicator.localScale = new Vector3(maxDiameter, maxDiameter, 1f);
                }
            }
        }
    }

    void CastCurrentSkill()
    {
        if (currentSkillIndex == -1) return;

        SkillData skill = equippedSkills[currentSkillIndex];
        
        // Get final target position
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Vector3 targetPoint = Vector3.zero;
        
        // Use LayerMask
        int layerMask = groundLayer.value != 0 ? groundLayer.value : Physics.DefaultRaycastLayers;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
        {
            targetPoint = hit.point;
        }
        else
        {
            // If Raycast misses, like pointing at the sky, then assume max distance
            Plane groundPlane = new Plane(Vector3.up, transform.position);
            float enter;
            if (groundPlane.Raycast(ray, out enter))
            {
                targetPoint = ray.GetPoint(enter);
            }
        }

        // Execute Cast Coroutine
        StartCoroutine(PerformCast(skill, targetPoint));
        
        // Set Cooldown
        currentCooldowns[currentSkillIndex] = skill.cooldown;
        
        // End Aiming
        CancelAiming();
    }

    IEnumerator PerformCast(SkillData skill, Vector3 targetPoint)
    {
        // 1. Lock movement
        if (movementScript) movementScript.SetLocked(true);

        // 2. Face target (Flip sprite instead of rotating transform)
        if (movementScript) movementScript.FaceTarget(targetPoint);

        // 3. Play Animation
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // 4. Wait for Cast Time
        if (skill.castTime > 0)
        {
            yield return new WaitForSeconds(skill.castTime);
        }

        // 5. Instantiate Magic Object
        if (skill.skillPrefab)
        {
            // Use player position + slight offset to avoid clipping ground
            Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
            
            GameObject magicObj = null;
            
            if (skill.aimType == SkillAimType.Directional)
            {
                // Spawn in front, rotate towards target
                Vector3 direction = targetPoint - spawnPos;
                direction.y = 0; // Flatten Y to ensure it travels horizontally (parallel to ground)
                
                if (direction != Vector3.zero)
                {
                    Quaternion rotation = Quaternion.LookRotation(direction);
                    // Apply additional rotation offset from SkillData
                    rotation *= Quaternion.Euler(skill.rotationOffset);
                    magicObj = Instantiate(skill.skillPrefab, spawnPos, rotation);
                }
            }
            else if (skill.aimType == SkillAimType.Positional)
            {
                // Spawn at mouse position
                Vector3 playerPos = transform.position;
                Vector3 flatTarget = new Vector3(targetPoint.x, playerPos.y, targetPoint.z);
                float dist = Vector3.Distance(playerPos, flatTarget);
                
                Vector3 finalPos = targetPoint;
                if (dist > skill.range)
                {
                    Vector3 dir = (flatTarget - playerPos).normalized;
                    finalPos = playerPos + dir * skill.range;
                    // Keep original Raycast height
                    finalPos.y = targetPoint.y; 
                }

                magicObj = Instantiate(skill.skillPrefab, finalPos, Quaternion.identity);
            }
            else if (skill.aimType == SkillAimType.Self)
            {
                // Spawn at player position
                magicObj = Instantiate(skill.skillPrefab, transform.position, Quaternion.identity);
            }

            // Initialize Projectile
            if (magicObj != null)
            {
                SpellProjectile proj = magicObj.GetComponent<SpellProjectile>();
                if (proj != null) 
                {
                    proj.SetCaster("Player");
                    // Force stationary for Self spells so they can hit the caster immediately
                    if (skill.aimType == SkillAimType.Self)
                    {
                        proj.isStationary = true;
                    }
                }
            }
        }

        Debug.Log($"Casted: {skill.skillName}");

        // 6. Unlock movement
        if (movementScript) movementScript.SetLocked(false);
    }
}