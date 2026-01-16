using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealingItem : DungeonItem
{
    public SkillData targetSkillData;
    public int chargesToAdd = 1;

    protected override void ApplyEffect(GameObject player)
    {
        PlayerSkillController skillController = player.GetComponent<PlayerSkillController>();
        
        if (skillController != null && targetSkillData != null)
        {
            // Use the skill name from the linked SkillData directly
            skillController.AddSkillCharge(targetSkillData.skillName, chargesToAdd);
        }
    }
}
