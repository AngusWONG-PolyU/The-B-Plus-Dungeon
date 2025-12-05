using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DungeonMinimap : MonoBehaviour
{
    [Header("References")]
    [System.NonSerialized]
    public BPlusTree<int, int> dungeonTree; // Reference to the B+ Tree structure for dungeon navigation
    public Canvas minimapCanvas; // UI Canvas for the minimap
    public Transform minimapContainer; // Container for minimap nodes
    
    [Header("Prefabs")]
    public GameObject nodeDisplayPrefab; // Prefab for displaying a node
    public GameObject connectionLinePrefab; // Prefab for connection lines
    
    [Header("Current Node")]
    public int targetKey; // Target key to search for in the tree
    public bool isAtLeaf = false; // True when player reaches a leaf node
    
    [Header("Visual Settings")]
    public Color currentNodeColor = new Color(1f, 0.9f, 0.2f, 1f); // Yellow for current
    public Color normalNodeColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Grey for all others
    public Color connectionLineColor = new Color(1f, 1f, 1f, 0.8f); // White
    public float nodeSpacing = 100f;
    public float levelSpacing = 80f;
    public Vector2 nodeSize = new Vector2(80f, 50f); // Width x Height (rectangle)
    public float outlineThickness = 2f; // Border thickness
    public bool autoScale = true; // Automatically scale to fit canvas
    public float canvasPadding = 20f; // Padding from canvas edges
    
    [Header("Background Settings")]
    public Sprite backgroundSprite;
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.5f);
    public float backgroundPadding = 10f;
    private GameObject backgroundObj;
    
    [Header("Full Map Settings")]
    public GameObject fullMapMessageText; // Text to show "Press M to see full tree"
    public bool isFullMapOpen = false;
    private HashSet<BPlusTreeNode<int, int>> visitedNodes = new HashSet<BPlusTreeNode<int, int>>();
    
    private List<GameObject> displayedNodes = new List<GameObject>();
    private List<GameObject> displayedLines = new List<GameObject>();
    [System.NonSerialized]
    private Dictionary<BPlusTreeNode<int, int>, GameObject> nodeToGameObject = new Dictionary<BPlusTreeNode<int, int>, GameObject>();
    [System.NonSerialized]
    private BPlusTreeNode<int, int> currentNode; // Current node player is at

    void Start()
    {
        if (minimapCanvas == null)
        {
            minimapCanvas = GetComponent<Canvas>();
            if (minimapCanvas == null) minimapCanvas = GetComponentInParent<Canvas>();
        }
        
        if (minimapContainer == null)
        {
            minimapContainer = transform;
        }
        
        // Ensure text is hidden at start
        if (fullMapMessageText != null)
        {
            fullMapMessageText.SetActive(false);
        }
        
        RefreshMinimap();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            isFullMapOpen = !isFullMapOpen;
            RefreshMinimap();
        }
    }

    // Called when player enters a child node (portal)
    public void EnterChildNode(int childIndex)
    {
        if (currentNode == null || currentNode.IsLeaf || childIndex >= currentNode.Children.Count)
        {
            Debug.LogWarning("Cannot enter child node - invalid state");
            return;
        }
        
        currentNode = currentNode.Children[childIndex];
        isAtLeaf = currentNode.IsLeaf;
        visitedNodes.Add(currentNode);
        isFullMapOpen = false;
        RefreshMinimap();
    }
    
    // Initialize the minimap with the tree and target
    public void InitializeDungeon(BPlusTree<int, int> tree, int target)
    {
        dungeonTree = tree;
        targetKey = target;
        currentNode = tree.Root;
        isAtLeaf = false;
        visitedNodes.Clear();
        if (currentNode != null) visitedNodes.Add(currentNode);
        RefreshMinimap();
    }

    // Refreshes the entire minimap display
    public void RefreshMinimap()
    {
        ClearMinimap();
        
        // Reset container state to ensure clean calculations
        minimapContainer.localScale = Vector3.one;
        minimapContainer.localRotation = Quaternion.identity;
        RectTransform containerRect = minimapContainer.GetComponent<RectTransform>();
        if (containerRect != null) containerRect.anchoredPosition = Vector2.zero;
        else minimapContainer.localPosition = Vector3.zero;
        
        if (dungeonTree == null || currentNode == null)
        {
            if (fullMapMessageText != null) fullMapMessageText.SetActive(false);
            Debug.LogWarning("DungeonMinimap: No tree to display");
            return;
        }

        // Update Text
        if (fullMapMessageText != null)
        {
            fullMapMessageText.SetActive(true);
            string msg = isFullMapOpen ? "Press M to close the full tree" : "Press M to see the full tree";
            
            Text txt = fullMapMessageText.GetComponent<Text>();
            if (txt != null)
            {
                txt.text = msg;
                txt.horizontalOverflow = HorizontalWrapMode.Overflow;
                txt.verticalOverflow = VerticalWrapMode.Overflow;
                txt.alignment = TextAnchor.MiddleCenter;
            }
            else
            {
                TMPro.TMP_Text tmp = fullMapMessageText.GetComponent<TMPro.TMP_Text>();
                if (tmp != null)
                {
                    tmp.text = msg;
                    tmp.enableWordWrapping = false;
                    tmp.alignment = TMPro.TextAlignmentOptions.Center;
                }
            }
        }

        if (isFullMapOpen)
        {
            DisplayFullTree();
            ScaleToFitCanvas(true); // Center it
        }
        else
        {
            if (isAtLeaf)
            {
                // No nodes displayed.
                // Just position the text.
                PositionTextOnly();
            }
            else
            {
                DisplayCurrentAndChildren(currentNode);
                ScaleToFitCanvas(false); // Top-right
            }
        }
    }

    // Display entire B+ tree structure from root
    private void DisplayFullTree()
    {
        if (dungeonTree == null || dungeonTree.Root == null) return;
        
        // Calculate subtree widths first to prevent overlap
        Dictionary<BPlusTreeNode<int, int>, float> subtreeWidths = new Dictionary<BPlusTreeNode<int, int>, float>();
        CalculateSubtreeWidth(dungeonTree.Root, subtreeWidths);
        
        DisplayTreeRecursive(dungeonTree.Root, 0, 0, subtreeWidths);
    }
    
    // Calculate width of each subtree
    private float CalculateSubtreeWidth(BPlusTreeNode<int, int> node, Dictionary<BPlusTreeNode<int, int>, float> widths)
    {
        if (node == null) return 0;
        
        float width = 0;
        
        if (node.IsLeaf || node.Children.Count == 0)
        {
            width = nodeSize.x + 20f;
        }
        else
        {
            foreach (var child in node.Children)
            {
                width += CalculateSubtreeWidth(child, widths);
            }
        }
        
        widths[node] = width;
        return width;
    }
    
    // Recursively display tree nodes
    private void DisplayTreeRecursive(BPlusTreeNode<int, int> node, int level, float xPosition, Dictionary<BPlusTreeNode<int, int>, float> subtreeWidths)
    {
        if (node == null) return;
        
        float yPosition = -level * levelSpacing;
        Vector2 position = new Vector2(xPosition, yPosition);
        
        // Determine if this node is current, has target, or normal
        bool isCurrent = (node == currentNode);
        bool hasTarget = ContainsTargetInSubtree(node);
        
        GameObject nodeObj = CreateNodeDisplay(node, position, isCurrent, hasTarget);
        nodeToGameObject[node] = nodeObj;
        
        // Display children
        if (!node.IsLeaf && node.Children.Count > 0)
        {
            float currentX = xPosition - (subtreeWidths[node] / 2f);
            
            foreach (var child in node.Children)
            {
                float childWidth = subtreeWidths[child];
                float childX = currentX + (childWidth / 2f);
                
                DisplayTreeRecursive(child, level + 1, childX, subtreeWidths);
                
                // Draw connection line
                if (nodeToGameObject.ContainsKey(child))
                {
                    Vector2 fromPos = nodeObj.GetComponent<RectTransform>().anchoredPosition;
                    Vector2 toPos = nodeToGameObject[child].GetComponent<RectTransform>().anchoredPosition;
                    Vector2 fromPoint = new Vector2(fromPos.x, fromPos.y - nodeSize.y / 2);
                    Vector2 toPoint = new Vector2(toPos.x, toPos.y + nodeSize.y / 2);
                    CreateConnectionLine(fromPoint, toPoint);
                }
                
                currentX += childWidth;
            }
        }
    }

    // Displays current node at bottom and children above (upside-down tree for intuitive navigation)
    private void DisplayCurrentAndChildren(BPlusTreeNode<int, int> node)
    {
        Vector2 currentPosition = new Vector2(0, -100);
        
        // Display current node at the bottom (where player is)
        GameObject currentNodeObj = CreateNodeDisplay(node, currentPosition, true);
        nodeToGameObject[node] = currentNodeObj;
        
        if (node.IsLeaf || node.Children == null) return;

        // Display children nodes above (upward = forward)
        int childCount = node.Children.Count;
        float totalWidth = (childCount - 1) * nodeSpacing;
        float startX = -totalWidth / 2f;
        
        for (int i = 0; i < node.Children.Count; i++)
        {
            Vector2 childPosition = new Vector2(startX + i * nodeSpacing, currentPosition.y + levelSpacing);
            BPlusTreeNode<int, int> child = node.Children[i];
            
            bool childHasTarget = ContainsTargetInSubtree(child);
            GameObject childObj = CreateNodeDisplay(child, childPosition, false, childHasTarget);
            nodeToGameObject[child] = childObj;
            
            // Draw connection line from edge to edge
            Vector2 currentPos = currentNodeObj.GetComponent<RectTransform>().anchoredPosition;
            Vector2 childPos = childObj.GetComponent<RectTransform>().anchoredPosition;
            
            // Calculate edge points (from top of current node to bottom of child node)
            Vector2 fromPoint = new Vector2(currentPos.x, currentPos.y + nodeSize.y / 2);
            Vector2 toPoint = new Vector2(childPos.x, childPos.y - nodeSize.y / 2);
            
            CreateConnectionLine(fromPoint, toPoint);
        }
    }
    
    // Check if a subtree contains the target key
    private bool ContainsTargetInSubtree(BPlusTreeNode<int, int> node)
    {
        if (node == null) return false;
        
        if (node.IsLeaf)
        {
            return node.Keys.Contains(targetKey);
        }
        
        // Check all children
        foreach (var child in node.Children)
        {
            if (ContainsTargetInSubtree(child))
            {
                return true;
            }
        }
        
        return false;
    }
    
    // Creates a visual display for a tree node
    private GameObject CreateNodeDisplay(BPlusTreeNode<int, int> node, Vector2 position, bool isCurrent, bool hasTarget = false)
    {
        GameObject nodeObj;
        
        if (nodeDisplayPrefab != null)
        {
            nodeObj = Instantiate(nodeDisplayPrefab, minimapContainer);
        }
        else
        {
            // Create hollow rectangle node
            nodeObj = new GameObject("Node");
            nodeObj.transform.SetParent(minimapContainer);
            
            RectTransform rect = nodeObj.AddComponent<RectTransform>();
            rect.sizeDelta = nodeSize;
            
            bool isVisited = visitedNodes.Contains(node);
            Color borderColor = isCurrent ? currentNodeColor : (isVisited ? currentNodeColor : normalNodeColor);
            
            // Create 4 border lines (top, bottom, left, right)
            CreateBorderLine(nodeObj, new Vector2(0, nodeSize.y / 2), new Vector2(nodeSize.x, outlineThickness), borderColor); // Top
            CreateBorderLine(nodeObj, new Vector2(0, -nodeSize.y / 2), new Vector2(nodeSize.x, outlineThickness), borderColor); // Bottom
            CreateBorderLine(nodeObj, new Vector2(-nodeSize.x / 2, 0), new Vector2(outlineThickness, nodeSize.y), borderColor); // Left
            CreateBorderLine(nodeObj, new Vector2(nodeSize.x / 2, 0), new Vector2(outlineThickness, nodeSize.y), borderColor); // Right
            
            // Add keys text inside the node
            GameObject keysTextObj = new GameObject("Keys");
            keysTextObj.transform.SetParent(nodeObj.transform);
            
            Text keysText = keysTextObj.AddComponent<Text>();
            keysText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            keysText.fontSize = 10;
            keysText.color = Color.white;
            keysText.alignment = TextAnchor.MiddleCenter;
            
            // Display keys
            string keysStr = string.Join(",", node.Keys);
            if (keysStr.Length > 15) keysStr = keysStr.Substring(0, 12) + "...";
            keysText.text = keysStr;
            
            RectTransform keysRect = keysTextObj.GetComponent<RectTransform>();
            keysRect.anchorMin = Vector2.zero;
            keysRect.anchorMax = Vector2.one;
            keysRect.sizeDelta = Vector2.zero;
        }
        
        RectTransform rectTransform = nodeObj.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        
        displayedNodes.Add(nodeObj);
        return nodeObj;
    }
    
    // Creates a border line for a hollow rectangle
    private void CreateBorderLine(GameObject parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject lineObj = new GameObject("Border");
        lineObj.transform.SetParent(parent.transform);
        
        Image line = lineObj.AddComponent<Image>();
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        line.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        line.color = color;
        
        RectTransform rect = lineObj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    // Creates a connection line between two nodes
    private void CreateConnectionLine(Vector3 from, Vector3 to)
    {
        GameObject lineObj;
        
        if (connectionLinePrefab != null)
        {
            lineObj = Instantiate(connectionLinePrefab, minimapContainer);
        }
        else
        {
            // Create default line using UI Image with white sprite
            lineObj = new GameObject("ConnectionLine");
            lineObj.transform.SetParent(minimapContainer);
            
            Image line = lineObj.AddComponent<Image>();
            line.color = connectionLineColor;
            
            // Create a simple white texture for the line
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            line.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }
        
        RectTransform rect = lineObj.GetComponent<RectTransform>();
        
        // Calculate position and rotation
        Vector2 fromPos = from;
        Vector2 toPos = to;
        Vector2 direction = toPos - fromPos;
        float distance = direction.magnitude;
        
        rect.anchoredPosition = (fromPos + toPos) / 2f;
        rect.sizeDelta = new Vector2(distance, 3f); // Slightly thicker line
        rect.rotation = Quaternion.FromToRotation(Vector3.right, direction);
        
        // Make sure line renders behind nodes
        lineObj.transform.SetAsFirstSibling();
        
        displayedLines.Add(lineObj);
    }

    // Clears all displayed minimap elements
    private void ClearMinimap()
    {
        if (backgroundObj != null) backgroundObj.SetActive(false);

        foreach (GameObject obj in displayedNodes)
        {
            Destroy(obj);
        }
        displayedNodes.Clear();
        
        foreach (GameObject obj in displayedLines)
        {
            Destroy(obj);
        }
        displayedLines.Clear();
        
        nodeToGameObject.Clear();
    }
    
    // Public method to clear minimap (called when exiting dungeon)
    public void ClearMinimapPublic()
    {
        if (fullMapMessageText != null) fullMapMessageText.SetActive(false);
        ClearMinimap();
        dungeonTree = null;
        currentNode = null;
        isAtLeaf = false;
        isFullMapOpen = false;
    }
    
    // Scale minimap to fit within canvas bounds and align to top-right corner
    private void ScaleToFitCanvas(bool center)
    {
        if (minimapCanvas == null || displayedNodes.Count == 0) return;
        
        RectTransform canvasRect = minimapCanvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.sizeDelta;
        
        // Calculate bounds of all displayed nodes
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        
        foreach (GameObject node in displayedNodes)
        {
            RectTransform rect = node.GetComponent<RectTransform>();
            Vector2 pos = rect.anchoredPosition;
            Vector2 size = rect.sizeDelta;
            
            minX = Mathf.Min(minX, pos.x - size.x / 2);
            maxX = Mathf.Max(maxX, pos.x + size.x / 2);
            minY = Mathf.Min(minY, pos.y - size.y / 2);
            maxY = Mathf.Max(maxY, pos.y + size.y / 2);
        }
        
        // Update background to fit content
        UpdateBackground(minX, maxX, minY, maxY);
        
        float contentWidth = maxX - minX;
        float contentHeight = maxY - minY;
        
        // Calculate scale to fit canvas with padding
        float availableWidth = canvasSize.x - (canvasPadding * 2);
        float availableHeight = canvasSize.y - (canvasPadding * 2);
        
        float scaleX = contentWidth > 0 ? availableWidth / contentWidth : 1f;
        float scaleY = contentHeight > 0 ? availableHeight / contentHeight : 1f;
        float scale = Mathf.Min(scaleX, scaleY, 1f); // Don't scale up, only down
        scale = Mathf.Max(scale, 0.1f); // Prevent too small scale
        
        // Apply scale to container
        minimapContainer.localScale = new Vector3(scale, scale, 1f);
        
        // Position at top-right corner with padding
        // Calculate scaled content dimensions
        float scaledWidth = contentWidth * scale;
        float scaledHeight = contentHeight * scale;
        
        float targetX, targetY;

        if (center)
        {
            // Center the content   
            targetX = -(minX + contentWidth / 2f) * scale;
            targetY = -(minY + contentHeight / 2f) * scale;
        }
        else
        {
            // Position so top-right of content aligns to top-right of canvas minus padding
            // Canvas top-right is at (canvasSize.x/2, canvasSize.y/2)
            float rightEdge = (canvasSize.x / 2) - canvasPadding;
            float topEdge = (canvasSize.y / 2) - canvasPadding;

            targetX = rightEdge - maxX * scale;
            targetY = topEdge - maxY * scale;
        }
        
        RectTransform containerRect = minimapContainer.GetComponent<RectTransform>();
        if (containerRect != null)
        {
            containerRect.anchoredPosition = new Vector2(targetX, targetY);
        }
        
        // Position Text
        if (fullMapMessageText != null)
        {
            RectTransform textRect = fullMapMessageText.GetComponent<RectTransform>();
            if (textRect != null)
            {
                // Reset text size to ensure consistency
                textRect.sizeDelta = new Vector2(300, 50);

                // Calculate bottom center of the map content
                float centerX = (minX + maxX) / 2f;
                float bottomY = minY;
                float padding = 30f;

                // It is a child, so coordinates are local to container
                // We need to counter-scale the text so it stays same size
                float invScale = scale > 0 ? 1f / scale : 1f;
                textRect.localScale = new Vector3(invScale, invScale, 1f);
                
                // Position relative to content bottom
                // We want it 'padding' units below bottomY visually
                // In local space, that is bottomY - (padding * invScale)
                textRect.anchoredPosition = new Vector2(centerX, bottomY - (padding * invScale));
            }
        }
    }

    private void PositionTextOnly()
    {
        if (minimapCanvas == null || fullMapMessageText == null) return;
        
        // Reset container
        minimapContainer.localScale = Vector3.one;
        
        RectTransform canvasRect = minimapCanvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.sizeDelta;
        
        // Top right
        float rightEdge = (canvasSize.x / 2) - canvasPadding;
        float topEdge = (canvasSize.y / 2) - canvasPadding;
        
        RectTransform containerRect = minimapContainer.GetComponent<RectTransform>();
        if (containerRect != null)
        {
            containerRect.anchoredPosition = new Vector2(rightEdge, topEdge);
        }
        else
        {
            // Fallback if container is just a Transform (though it should be RectTransform in UI)
            minimapContainer.localPosition = new Vector3(rightEdge, topEdge, 0);
        }
        
        RectTransform textRect = fullMapMessageText.GetComponent<RectTransform>();
        if (textRect != null)
        {
            textRect.localScale = Vector3.one;
            
            // Ensure text is wide enough
            Vector2 textSize = new Vector2(300, 50);
            textRect.sizeDelta = textSize;
            
            // Position text slightly down and left from the corner anchor
            Vector2 textPos = new Vector2(-textSize.x / 2, -textSize.y / 2);
            textRect.anchoredPosition = textPos;
            
            // Add background behind text
            float minX = textPos.x - textSize.x / 2;
            float maxX = textPos.x + textSize.x / 2;
            float minY = textPos.y - textSize.y / 2;
            float maxY = textPos.y + textSize.y / 2;
            
            UpdateBackground(minX, maxX, minY, maxY);
        }
    }

    private void UpdateBackground(float minX, float maxX, float minY, float maxY)
    {
        if (backgroundObj == null)
        {
            backgroundObj = new GameObject("MapBackground");
            backgroundObj.transform.SetParent(minimapContainer, false);
            Image img = backgroundObj.AddComponent<Image>();
            img.color = backgroundColor;
            if (backgroundSprite != null) img.sprite = backgroundSprite;
            else 
            {
                // Create default white sprite if none provided
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                img.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            }
        }
        
        // Ensure it's at the back
        backgroundObj.transform.SetAsFirstSibling();
        backgroundObj.SetActive(true);
        
        RectTransform rect = backgroundObj.GetComponent<RectTransform>();
        float width = (maxX - minX) + (backgroundPadding * 2);
        float height = (maxY - minY) + (backgroundPadding * 2);
        float centerX = (minX + maxX) / 2f;
        float centerY = (minY + maxY) / 2f;
        
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(centerX, centerY);
    }
}