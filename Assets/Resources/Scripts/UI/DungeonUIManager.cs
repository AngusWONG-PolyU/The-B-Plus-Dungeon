using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DungeonUIManager : MonoBehaviour
{
    [Header("HUD Elements")]
    public TextMeshProUGUI targetText;
    public GameObject minimapContainer; // Reference to the Minimap Container
    public Toggle minimapToggle;

    public void SetTargetText(string text)
    {
        if (targetText != null) targetText.text = text;
    }
    
    public void SetTargetColor(Color color)
    {
        if (targetText != null) targetText.color = color;
    }

    public void SetTargetVisibility(bool visible)
    {
        if (targetText != null) targetText.gameObject.SetActive(visible);
    }

    public void SetMinimapVisibility(bool visible)
    {
        if (minimapContainer != null) minimapContainer.SetActive(visible);
    }

    public void SetMinimapToggleVisibility(bool visible)
    {
        if (minimapToggle != null)
        {
            minimapToggle.SetIsOnWithoutNotify(true);
            minimapToggle.gameObject.SetActive(visible);
        }
    }
}