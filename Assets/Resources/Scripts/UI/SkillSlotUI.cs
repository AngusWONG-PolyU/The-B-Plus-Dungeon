using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillSlotUI : MonoBehaviour
{
    [Header("UI Components")]
    public Image skillIcon;
    public Image cooldownOverlay; // Set Image Type to 'Filled' in Editor
    public Text cooldownText;
    public GameObject selectionOutline;

    public void Setup(SkillData skill)
    {
        if (skill != null)
        {
            if (skillIcon)
            {
                skillIcon.sprite = skill.icon;
                skillIcon.enabled = true;
            }
            if (cooldownOverlay) cooldownOverlay.fillAmount = 0;
            if (cooldownText) cooldownText.text = "";
        }
        else
        {
            if (skillIcon) skillIcon.enabled = false;
            if (cooldownOverlay) cooldownOverlay.fillAmount = 0;
            if (cooldownText) cooldownText.text = "";
        }
    }

    public void UpdateCooldown(float current, float max)
    {
        if (cooldownOverlay)
        {
            if (max > 0 && current > 0)
                cooldownOverlay.fillAmount = current / max;
            else
                cooldownOverlay.fillAmount = 0;
        }

        if (cooldownText)
        {
            if (current > 0)
                cooldownText.text = Mathf.Ceil(current).ToString();
            else
                cooldownText.text = "";
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (selectionOutline) selectionOutline.SetActive(isSelected);
    }
}
