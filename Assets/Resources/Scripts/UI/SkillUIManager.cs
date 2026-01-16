using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillUIManager : MonoBehaviour
{
    [Header("References")]
    public PlayerSkillController playerSkillController;
    public SkillSlotUI[] skillSlots;

    void Start()
    {
        if (playerSkillController == null)
            playerSkillController = FindObjectOfType<PlayerSkillController>();

        InitializeSlots();
    }

    void InitializeSlots()
    {
        if (playerSkillController == null) return;

        for (int i = 0; i < skillSlots.Length; i++)
        {
            if (i < playerSkillController.equippedSkills.Length)
            {
                skillSlots[i].Setup(playerSkillController.equippedSkills[i]);
            }
            else
            {
                skillSlots[i].Setup(null);
            }
        }
    }

    void Update()
    {
        if (playerSkillController == null) return;

        for (int i = 0; i < skillSlots.Length; i++)
        {
            if (i < playerSkillController.equippedSkills.Length)
            {
                float currentCD = playerSkillController.GetCurrentCooldown(i);
                float maxCD = playerSkillController.equippedSkills[i].cooldown;
                
                skillSlots[i].UpdateCooldown(currentCD, maxCD);

                // Update Charges
                int charges = playerSkillController.GetCurrentCharges(i);
                bool hasLimit = playerSkillController.equippedSkills[i].hasUsageLimit;
                skillSlots[i].UpdateCharges(charges, hasLimit);
                
                // Highlight selected skill
                skillSlots[i].SetSelected(playerSkillController.GetCurrentSkillIndex() == i);
            }
        }
    }
}