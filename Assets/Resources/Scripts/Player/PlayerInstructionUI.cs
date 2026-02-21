using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerInstructionUI : MonoBehaviour
{
    public static PlayerInstructionUI Instance { get; private set; }

    [Header("UI Reference")]
    public TextMeshProUGUI instructionText;
    
    private Coroutine hideCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Ensure text is initially hidden
        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(false);
        }
    }

    public void ShowInstruction(string text)
    {
        if (instructionText != null)
        {
            if (hideCoroutine != null) StopCoroutine(hideCoroutine);
            instructionText.text = text;
            instructionText.gameObject.SetActive(true);
        }
    }

    public void ShowInstruction(string text, float duration)
    {
        ShowInstruction(text);
        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideAfterDelay(duration));
    }

    public void HideInstruction()
    {
        if (hideCoroutine != null) 
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(false);
        }
    }
    
    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideInstruction();
    }
}
