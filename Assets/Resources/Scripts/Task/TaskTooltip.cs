using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class TaskTooltip : MonoBehaviour
{
    public static TaskTooltip Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private RectTransform backgroundRectTransform;
    [SerializeField] private RectTransform canvasRectTransform;
    [SerializeField] private LayoutElement layoutElement;
    
    [SerializeField] private int characterWrapLimit = 80;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
            
        // Attempt to find canvas
        Canvas pCanvas = GetComponentInParent<Canvas>();
        if (pCanvas != null)
        {
            canvasRectTransform = pCanvas.GetComponent<RectTransform>();
        }
        else
        {
            // Fallback to finding any canvas in scene
            Canvas anyCanvas = FindObjectOfType<Canvas>();
            if (anyCanvas != null) canvasRectTransform = anyCanvas.GetComponent<RectTransform>();
        }
        
        HideTooltip();
    }

    private void Update()
    {
        if (gameObject.activeSelf && canvasRectTransform != null)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform, 
                Input.mousePosition, 
                null, // Use null for overlay canvas, or eventData.pressEventCamera if ScreenSpaceCamera
                out localPoint);
                
            // Offset from the cursor (moved up and slightly right so the mouse doesn't block text)
            localPoint.x += 15f;
            localPoint.y += 30f;
            
            // Ensure tooltip stays on screen
            float pivotX = backgroundRectTransform.pivot.x;
            float pivotY = backgroundRectTransform.pivot.y;
            
            float rightEdge = localPoint.x + (1f - pivotX) * backgroundRectTransform.rect.width;
            float leftEdge = localPoint.x - pivotX * backgroundRectTransform.rect.width;
            float topEdge = localPoint.y + (1f - pivotY) * backgroundRectTransform.rect.height;
            float bottomEdge = localPoint.y - pivotY * backgroundRectTransform.rect.height;
            
            Rect canvasRect = canvasRectTransform.rect;
            
            if (rightEdge > canvasRect.xMax)
                localPoint.x -= (rightEdge - canvasRect.xMax);
            if (leftEdge < canvasRect.xMin)
                localPoint.x += (canvasRect.xMin - leftEdge);
            if (bottomEdge < canvasRect.yMin)
                localPoint.y += (canvasRect.yMin - bottomEdge);
            if (topEdge > canvasRect.yMax)
                localPoint.y -= (topEdge - canvasRect.yMax);
                
            GetComponent<RectTransform>().anchoredPosition = localPoint;
        }
    }

    public void ShowTooltip(string tooltipString)
    {
        gameObject.SetActive(true);
        tooltipText.text = tooltipString;
        
        int headerLength = tooltipText.text.Length;
        layoutElement.enabled = (headerLength > characterWrapLimit) ? true : false;
        
        // Force update to get the correct size immediately
        LayoutRebuilder.ForceRebuildLayoutImmediate(backgroundRectTransform);
    }

    public void HideTooltip()
    {
        gameObject.SetActive(false);
    }
}