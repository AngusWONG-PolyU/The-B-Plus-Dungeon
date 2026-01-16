using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicScaleItem : DungeonItem
{
    [Header("Magic Scale Settings")]
    public float scaleIncreaseAmount = 0.5f; // Increase scale by 50%

    protected override void ApplyEffect(GameObject player)
    {
        PlayerSkillController skillController = player.GetComponent<PlayerSkillController>();
        
        if (skillController != null)
        {
            skillController.IncreaseMagicScale(scaleIncreaseAmount);
            Debug.Log($"{itemName} applied. Magic scale increased by {scaleIncreaseAmount}.");
        }
        else
        {
            Debug.LogWarning("PlayerSkillController not found on player!");
        }
    }
}
