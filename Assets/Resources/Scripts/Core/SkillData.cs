using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SkillAimType
{
    Directional, // Projectile type: Casts toward the mouse direction
    Positional, // Point type: Casts at mouse position
    Self // Self type: Casts immediately on self
}

[CreateAssetMenu(fileName = "New Skill", menuName = "Magic/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Basic Info")]
    public string skillName;
    public Sprite icon;
    [TextArea] public string description;

    [Header("Settings")]
    public SkillAimType aimType;
    public bool isHealing = false;
    public float cooldown = 1f;
    public float range = 10f;
    public float castTime = 0.2f; // Casting animation time

    [Header("Visuals & Prefabs")]
    public GameObject skillPrefab; // The spell prefab itself
    public Vector3 rotationOffset; // Adjust rotation if the prefab faces the wrong way

    [Header("Usage Limits (Optional)")]
    public bool hasUsageLimit = false; 
    [Tooltip("Initial max charges for this skill. -1 for infinite.")]
    public int maxCharges = -1; 
}