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
    public Toggle minimapToggle; // Toggle to show/hide the minimap
    
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
    public int normalFontSize = 10;
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
    public Vector2 fullMapNodeSize = new Vector2(160f, 100f);
    public int fullMapFontSize = 20;
    public float fullMapLevelSpacing = 160f;
    public float fullMapInitialZoom = 0.5f;
    private HashSet<BPlusTreeNode<int, int>> visitedNodes = new HashSet<BPlusTreeNode<int, int>>();

    [Header("Zoom Pan Settings")]
    public float minZoom = 0.1f;
    public float maxZoom = 2.0f;
    public float zoomSensitivity = 1.0f;
    public float panSensitivity = 3.0f;
    
    // Control how tight the tree is packed
    public float treeChildPadding = 10f; // Gap between leaf nodes in full map
    
    private List<GameObject> displayedNodes = new List<GameObject>();
    private List<GameObject> displayedLines = new List<GameObject>();
    [System.NonSerialized]
    private Dictionary<BPlusTreeNode<int, int>, GameObject> nodeToGameObject = new Dictionary<BPlusTreeNode<int, int>, GameObject>();
    [System.NonSerialized]
    private BPlusTreeNode<int, int> currentNode; // Current node player is at

    private Vector2 CurrentNodeSize => isFullMapOpen ? fullMapNodeSize : nodeSize;
    private int CurrentFontSize => isFullMapOpen ? fullMapFontSize : normalFontSize;
    private float CurrentLevelSpacing => isFullMapOpen ? fullMapLevelSpacing : levelSpacing;

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
        
        if (minimapToggle != null)
        {
            minimapToggle.onValueChanged.AddListener(OnMinimapToggleChanged);
            minimapToggle.gameObject.SetActive(false);
        }
        
        // Ensure text is hidden at start
        if (fullMapMessageText != null)
        {
            fullMapMessageText.SetActive(false);
        }
        
        RefreshMinimap();
    }

    private void OnMinimapToggleChanged(bool isOn)
    {
        if (minimapContainer != null)
        {
            minimapContainer.gameObject.SetActive(isOn);
        }
        
        if (backgroundObj != null)
        {
            backgroundObj.SetActive(isOn);
        }
        
        if (fullMapMessageText != null)
        {
            fullMapMessageText.SetActive(isOn && gameObject.activeInHierarchy);
        }
        
        if (isOn)
        {
            RefreshMinimap();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            isFullMapOpen = !isFullMapOpen;
            minimapToggle.SetIsOnWithoutNotify(true);
            minimapToggle.gameObject.SetActive(!isFullMapOpen);
            RefreshMinimap();
        }

        if (isFullMapOpen)
        {
            HandleFullMapInput();
        }

        // Continuous check to sync text visibility with minimap container visibility
        if (fullMapMessageText != null && minimapContainer != null)
        {
            // If the minimap content is active, the text should be active too
            bool shouldShow = minimapContainer.gameObject.activeInHierarchy && gameObject.activeInHierarchy;
            if (minimapToggle != null && !minimapToggle.isOn)
            {
                shouldShow = false;
            }
            if (fullMapMessageText.activeSelf != shouldShow)
            {
                fullMapMessageText.SetActive(shouldShow);
            }
        }
    }

    private bool isDragging = false;
    private bool isValidDrag = false;
    private Vector3 dragOrigin;

    private void HandleFullMapInput()
    {
        // Zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            float currentScale = minimapContainer.localScale.x;
            float targetScale = currentScale + scroll * zoomSensitivity;
            targetScale = Mathf.Clamp(targetScale, minZoom, maxZoom);
            
            // Zoom towards mouse position
            RectTransform rect = minimapContainer.GetComponent<RectTransform>();
            if (rect != null && rect.parent != null)
            {
                Vector2 mousePosInParent;
                RectTransform parentRect = rect.parent as RectTransform;
                Camera cam = minimapCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : minimapCanvas.worldCamera;
                
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, Input.mousePosition, cam, out mousePosInParent))
                {
                    Vector2 direction = mousePosInParent - rect.anchoredPosition;
                    float scaleRatio = targetScale / currentScale;
                    minimapContainer.localScale = new Vector3(targetScale, targetScale, 1f);
                    rect.anchoredPosition = mousePosInParent - (direction * scaleRatio);
                }
            }
            else
            {
                minimapContainer.localScale = new Vector3(targetScale, targetScale, 1f);
            }
        }

        // Pan
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            isDragging = false;
            isValidDrag = false;
            
            Camera cam = minimapCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : minimapCanvas.worldCamera;
            
            // Prioritize checking backgroundObj as it represents the content area
            RectTransform checkRect = null;
            if (backgroundObj != null && backgroundObj.activeSelf)
                checkRect = backgroundObj.GetComponent<RectTransform>();
            else if (minimapContainer != null)
                checkRect = minimapContainer.GetComponent<RectTransform>();
                
            if (checkRect != null && RectTransformUtility.RectangleContainsScreenPoint(checkRect, Input.mousePosition, cam))
            {
                isValidDrag = true;
                dragOrigin = Input.mousePosition;
            }
        }

        if (isValidDrag && (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2)))
        {
            if (!isDragging && Vector3.Distance(dragOrigin, Input.mousePosition) > 5f)
            {
                isDragging = true;
            }

            if (isDragging)
            {
                float moveX = Input.GetAxis("Mouse X");
                float moveY = Input.GetAxis("Mouse Y");
                Vector2 movement = new Vector2(moveX, moveY) * panSensitivity * 15f; 

                RectTransform rect = minimapContainer.GetComponent<RectTransform>();
                if (rect != null) rect.anchoredPosition += movement;
                else minimapContainer.localPosition += (Vector3)movement;
            }
        }
        
        if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
        {
            isDragging = false;
            isValidDrag = false;
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
        
        if (minimapToggle != null)
        {
            minimapToggle.gameObject.SetActive(true);
            minimapToggle.isOn = true;
        }
        
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
            // Only show text if the minimap container is active in hierarchy
            bool isMapVisible = (minimapContainer != null && minimapContainer.gameObject.activeInHierarchy);
            bool shouldShow = isMapVisible && gameObject.activeInHierarchy;
            if (minimapToggle != null && !minimapToggle.isOn)
            {
                shouldShow = false;
            }
            
            fullMapMessageText.SetActive(shouldShow);
            
            if (shouldShow)
            {
                // Ensure text is on the canvas
                if (minimapCanvas != null && fullMapMessageText.transform.parent != minimapCanvas.transform)
                {
                    fullMapMessageText.transform.SetParent(minimapCanvas.transform);
                }
                
                fullMapMessageText.transform.SetAsLastSibling(); // Ensure it is on the top
                
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
                
                // Set size for the text background
                RectTransform textRect = fullMapMessageText.GetComponent<RectTransform>();
                textRect.sizeDelta = new Vector2(240f, 40f); 
            }
        }

        if (isFullMapOpen)
        {
            DisplayFullTree();
            
            // Set initial zoom for full map
            minimapContainer.localScale = new Vector3(fullMapInitialZoom, fullMapInitialZoom, 1f);
            
            UpdateContentBounds(false); // Update background bounds only
            
            // Center on Current Node
            if (currentNode != null && nodeToGameObject.ContainsKey(currentNode))
            {
                RectTransform nodeRect = nodeToGameObject[currentNode].GetComponent<RectTransform>();
                Vector2 nodePos = nodeRect.anchoredPosition;
                
                if (containerRect != null)
                {
                    containerRect.anchoredPosition = -nodePos * fullMapInitialZoom;
                }
            }
            else
            {
                 UpdateContentBounds(true); // Fallback to centering content
            }
            
            // Position Text for Full Map (Bottom Center)
            if (fullMapMessageText != null)
            {
                RectTransform textRect = fullMapMessageText.GetComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0.5f, 0f);
                textRect.anchorMax = new Vector2(0.5f, 0f);
                textRect.pivot = new Vector2(0.5f, 0f);
                textRect.anchoredPosition = new Vector2(0, 50f); // 50px from bottom
            }
        }
        else
        {
            if (isAtLeaf)
            {
                PositionTextOnly();
            }
            else
            {
                DisplayCurrentAndChildren(currentNode);
                FitMinimapToScreen(); // Scale and position for the minimap
            }
        }
    }

    // Display entire B+ tree structure from root
    private void DisplayFullTree()
    {
        if (dungeonTree == null || dungeonTree.Root == null) return;
        
        // Calculate subtree widths
        Dictionary<BPlusTreeNode<int, int>, float> subtreeWidths = new Dictionary<BPlusTreeNode<int, int>, float>();
        CalculateSubtreeWidth(dungeonTree.Root, subtreeWidths);
        
        DisplayTreeRecursive(dungeonTree.Root, 0, 0, subtreeWidths);
    }
    
    // Calculate the width of each subtree
    private float CalculateSubtreeWidth(BPlusTreeNode<int, int> node, Dictionary<BPlusTreeNode<int, int>, float> widths)
    {
        if (node == null) return 0;
        
        float width = 0;
        
        if (node.IsLeaf || node.Children.Count == 0)
        {
            width = CurrentNodeSize.x + treeChildPadding;
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
        
        float yPosition = -level * CurrentLevelSpacing;
        Vector2 position = new Vector2(xPosition, yPosition);
        
        // Determine if this node is current or normal
        bool isCurrent = (node == currentNode);
        
        GameObject nodeObj = CreateNodeDisplay(node, position, isCurrent);
        nodeToGameObject[node] = nodeObj;
        
        // Display children
        if (!node.IsLeaf && node.Children.Count > 0)
        {
            float currentX = xPosition - (subtreeWidths[node] / 2f);
            List<Vector2> childPositions = new List<Vector2>();
            
            foreach (var child in node.Children)
            {
                float childWidth = subtreeWidths[child];
                float childX = currentX + (childWidth / 2f);
                
                DisplayTreeRecursive(child, level + 1, childX, subtreeWidths);
                
                if (nodeToGameObject.ContainsKey(child))
                {
                    childPositions.Add(nodeToGameObject[child].GetComponent<RectTransform>().anchoredPosition);
                }
                
                currentX += childWidth;
            }
            
            // Draw Orthogonal Bus Connections
            if (childPositions.Count > 0)
            {
                Vector2 parentPos = nodeObj.GetComponent<RectTransform>().anchoredPosition;
                Vector2 parentBottom = new Vector2(parentPos.x, parentPos.y - CurrentNodeSize.y / 2);
                float midY = (parentBottom.y + (childPositions[0].y + CurrentNodeSize.y / 2)) / 2f;
                
                // 1. Vertical from Parent to Mid
                CreateConnectionLine(parentBottom, new Vector2(parentBottom.x, midY));
                
                // 2. Horizontal Bus from First Child to Last Child
                float minChildX = childPositions[0].x;
                float maxChildX = childPositions[childPositions.Count - 1].x;

                CreateConnectionLine(new Vector2(minChildX, midY), new Vector2(maxChildX, midY));
                
                // 3. Vertical from Mid to each Child
                foreach (Vector2 childPos in childPositions)
                {
                    Vector2 childTop = new Vector2(childPos.x, childPos.y + CurrentNodeSize.y / 2);
                    CreateConnectionLine(new Vector2(childPos.x, midY), childTop);
                }
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
            Vector2 childPosition = new Vector2(startX + i * nodeSpacing, currentPosition.y + CurrentLevelSpacing);
            BPlusTreeNode<int, int> child = node.Children[i];
            
            GameObject childObj = CreateNodeDisplay(child, childPosition, false);
            nodeToGameObject[child] = childObj;
            
            // Draw connection line from edge to edge
            Vector2 currentPos = currentNodeObj.GetComponent<RectTransform>().anchoredPosition;
            Vector2 childPos = childObj.GetComponent<RectTransform>().anchoredPosition;
            
            // Calculate edge points (from top of current node to bottom of child node)
            Vector2 fromPoint = new Vector2(currentPos.x, currentPos.y + CurrentNodeSize.y / 2);
            Vector2 toPoint = new Vector2(childPos.x, childPos.y - CurrentNodeSize.y / 2);
            
            CreateConnectionLine(fromPoint, toPoint);
        }
    }
    
    // Creates a visual display for a tree node
    private GameObject CreateNodeDisplay(BPlusTreeNode<int, int> node, Vector2 position, bool isCurrent)
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
            rect.sizeDelta = CurrentNodeSize;
            
            bool isVisited = visitedNodes.Contains(node);
            Color borderColor = isCurrent ? currentNodeColor : (isVisited ? currentNodeColor : normalNodeColor);
            
            // Create 4 border lines (top, bottom, left, right)
            CreateBorderLine(nodeObj, new Vector2(0, CurrentNodeSize.y / 2), new Vector2(CurrentNodeSize.x, outlineThickness), borderColor); // Top
            CreateBorderLine(nodeObj, new Vector2(0, -CurrentNodeSize.y / 2), new Vector2(CurrentNodeSize.x, outlineThickness), borderColor); // Bottom
            CreateBorderLine(nodeObj, new Vector2(-CurrentNodeSize.x / 2, 0), new Vector2(outlineThickness, CurrentNodeSize.y), borderColor); // Left
            CreateBorderLine(nodeObj, new Vector2(CurrentNodeSize.x / 2, 0), new Vector2(outlineThickness, CurrentNodeSize.y), borderColor); // Right
            
            // Add keys text inside the node
            GameObject keysTextObj = new GameObject("Keys");
            keysTextObj.transform.SetParent(nodeObj.transform);
            
            Text keysText = keysTextObj.AddComponent<Text>();
            keysText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            keysText.fontSize = CurrentFontSize;
            keysText.color = Color.white;
            keysText.alignment = TextAnchor.MiddleCenter;
            keysText.horizontalOverflow = HorizontalWrapMode.Wrap;
            
            // Display keys
            string keysStr = string.Join(",", node.Keys);
            
            // Only truncate if it is not in full map mode
            if (!isFullMapOpen && keysStr.Length > 15) 
            {
                keysStr = keysStr.Substring(0, 12) + "...";
            }
            
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
        if (minimapToggle != null) minimapToggle.gameObject.SetActive(false);
        ClearMinimap();
        dungeonTree = null;
        currentNode = null;
        isAtLeaf = false;
        isFullMapOpen = false;
    }
    
    private void FitMinimapToScreen()
    {
        if (minimapCanvas == null || displayedNodes.Count == 0) return;
        
        RectTransform canvasRect = minimapCanvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.sizeDelta;
        
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
        
        UpdateBackground(minX, maxX, minY, maxY);
        
        float contentWidth = maxX - minX;
        float contentHeight = maxY - minY;
        
        float availableWidth = canvasSize.x - (canvasPadding * 2);
        float availableHeight = canvasSize.y - (canvasPadding * 2);
        
        float scaleX = contentWidth > 0 ? availableWidth / contentWidth : 1f;
        float scaleY = contentHeight > 0 ? availableHeight / contentHeight : 1f;
        float scale = Mathf.Min(scaleX, scaleY, 1f); 
        scale = Mathf.Max(scale, 0.1f);
        
        minimapContainer.localScale = new Vector3(scale, scale, 1f);
        
        float rightEdge = (canvasSize.x / 2) - canvasPadding;
        float topEdge = (canvasSize.y / 2) - canvasPadding;
        
        if (minimapToggle != null)
        {
            topEdge -= 15f;
        }

        float targetX = rightEdge - maxX * scale;
        float targetY = topEdge - maxY * scale;
        
        RectTransform containerRect = minimapContainer.GetComponent<RectTransform>();
        if (containerRect != null) containerRect.anchoredPosition = new Vector2(targetX, targetY);
        
        // Position Text below the minimap
        if (fullMapMessageText != null)
        {
            RectTransform textRect = fullMapMessageText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            
            float mapBottomY = targetY + (minY * scale);
            float textY = mapBottomY - 30f; // 30 units padding below map
            
            // Align text with center of map horizontally
            float mapCenterX = targetX + ((minX + maxX) / 2f * scale);
            
            textRect.anchoredPosition = new Vector2(mapCenterX, textY);
        }
    }
    
    private void UpdateContentBounds(bool centerContainer)
    {
        if (displayedNodes.Count == 0) return;
        
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
        
        UpdateBackground(minX, maxX, minY, maxY);
        
        if (centerContainer)
        {
            float contentWidth = maxX - minX;
            float contentHeight = maxY - minY;
            float targetX = -(minX + contentWidth / 2f);
            float targetY = -(minY + contentHeight / 2f);
            
            RectTransform containerRect = minimapContainer.GetComponent<RectTransform>();
            if (containerRect != null) containerRect.anchoredPosition = new Vector2(targetX, targetY);
        }
    }

    private void PositionTextOnly()
    {
        if (minimapCanvas == null || fullMapMessageText == null) return;
        
        // Reset container
        minimapContainer.localScale = Vector3.one;
        minimapContainer.localRotation = Quaternion.identity;
        minimapContainer.localPosition = Vector3.zero;
        if (backgroundObj != null) backgroundObj.SetActive(false);
        
        // Position fullMapMessageText at Top Right
        RectTransform canvasRect = minimapCanvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.sizeDelta;
        
        float rightEdge = (canvasSize.x / 2) - canvasPadding;
        float topEdge = (canvasSize.y / 2) - canvasPadding;
        
        if (minimapToggle != null)
        {
            topEdge -= 15f;
        }
        
        RectTransform textRect = fullMapMessageText.GetComponent<RectTransform>();
        if (textRect != null)
        {
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(1f, 1f); // Top Right pivot
            textRect.anchoredPosition = new Vector2(rightEdge, topEdge);
            textRect.localScale = Vector3.one;
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
        
        // Ensure it is at the back
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