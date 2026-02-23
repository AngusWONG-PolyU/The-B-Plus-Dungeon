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
    private Transform originalParent;

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
            originalParent = instructionText.transform.parent;
        }
    }

    private void Update()
    {
        if (instructionText != null && instructionText.gameObject.activeSelf)
        {
            // Check if TaskCanvas is active
            if (BPlusTreeTaskManager.Instance != null && 
                BPlusTreeTaskManager.Instance.taskCanvas != null && 
                BPlusTreeTaskManager.Instance.taskCanvas.activeInHierarchy)
            {
                // Move to TaskCanvas if not already there
                if (instructionText.transform.parent != BPlusTreeTaskManager.Instance.taskCanvas.transform)
                {
                    // Use true to keep world position/scale, then reset local position
                    instructionText.transform.SetParent(BPlusTreeTaskManager.Instance.taskCanvas.transform, true);
                    instructionText.transform.localPosition = Vector3.zero; // Center it
                    instructionText.transform.SetAsLastSibling(); // Ensure it's on top within TaskCanvas
                }
            }
            else
            {
                // Return to original parent if TaskCanvas is not active
                if (instructionText.transform.parent != originalParent)
                {
                    // Use true to keep world position/scale, then reset local position
                    instructionText.transform.SetParent(originalParent, true);
                    instructionText.transform.localPosition = Vector3.zero; // Center it
                }
            }
        }
    }

    public void ShowInstruction(string text)
    {
        if (instructionText != null)
        {
            if (hideCoroutine != null) StopCoroutine(hideCoroutine);
            instructionText.text = text;
            
            // Default color
            if (ColorUtility.TryParseHtmlString("#FFD700", out Color defaultColor))
            {
                instructionText.color = defaultColor;
            }
            
            instructionText.gameObject.SetActive(true);
        }
    }

    public void ShowInstruction(string text, float duration, bool isError = false)
    {
        ShowInstruction(text);
        if (isError && instructionText != null)
        {
            instructionText.color = Color.red;
        }
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
        yield return new WaitForSecondsRealtime(delay);
        HideInstruction();
    }
}
