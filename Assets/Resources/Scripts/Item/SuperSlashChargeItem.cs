using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuperSlashChargeItem : DungeonItem
{
    [Header("Super Slash Setup")]
    public SkillData superSlashSkillData;
    
    public int chargesToAdd = 1;

    protected override void ApplyEffect(GameObject player)
    {
        PlayerSkillController skillController = player.GetComponent<PlayerSkillController>();
        
        if (skillController != null && superSlashSkillData != null)
        {
            // Add charge to the super slash skill
            skillController.AddSkillCharge(superSlashSkillData.skillName, chargesToAdd);
            Debug.Log($"[SuperSlashChargeItem] Added {chargesToAdd} charge(s) to {superSlashSkillData.skillName}!");
        }
    }
}