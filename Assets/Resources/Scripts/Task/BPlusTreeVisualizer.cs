using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BPlusTreeVisualizer : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject nodePrefab;
    public GameObject linkLinePrefab; 
    
    [Header("Container")]
    public RectTransform treeContainer; 

    [Header("Layout Settings")]
    public float levelHeight = 180f; // Vertical distance
    public float nodeDataWidth = 60f; // Approximate width per key
    public float nodePadding = 40f; // Gap between nodes

    private Dictionary<BPlusTreeNode<int, string>, RectTransform> _nodeMap;

    public void RenderTree(BPlusTree<int, string> tree)
    {
        foreach (Transform child in treeContainer) Destroy(child.gameObject);
        _nodeMap = new Dictionary<BPlusTreeNode<int, string>, RectTransform>();

        if (tree.Root == null) return;

        // 1. Spawn all visual nodes first to get their RectTransforms ready
        SpawnNodesRecursive(tree.Root);
        
        // 2. Layout Leaves First
        LayoutLeaves(tree);

        // 3. Layout Internal Nodes (Bottom-Up)
        LayoutInternalNodes(tree.Root);
        
        // 4. Center and Scale the whole tree
        CenterAndFitTree();

        // 5. Draw Connections
        DrawConnectionsRecursive(tree.Root);
    }

    private void SpawnNodesRecursive(BPlusTreeNode<int, string> node)
    {
        GameObject nodeObj = Instantiate(nodePrefab, treeContainer);
        BPlusTreeVisualNode visualNode = nodeObj.GetComponent<BPlusTreeVisualNode>();
        visualNode.Initialize(node);
        
        // Force update layout immediately to get the correct width
        LayoutRebuilder.ForceRebuildLayoutImmediate(nodeObj.GetComponent<RectTransform>());

        _nodeMap[node] = nodeObj.GetComponent<RectTransform>();

        if (!node.IsLeaf && node.Children != null)
        {
            foreach (var child in node.Children) SpawnNodesRecursive(child);
        }
    }

    private void LayoutLeaves(BPlusTree<int, string> tree)
    {
        // Use the linked list property of B+ Tree leaves
        BPlusTreeNode<int, string> current = tree.FirstLeaf;
        float currentX = 0f;

        while (current != null)
        {
            if (!_nodeMap.ContainsKey(current))
            {
                current = current.Next;
                continue;
            }

            RectTransform rt = _nodeMap[current];
            int depth = GetDepth(current); 
            
            // Force rebuild to get the size from ContentSizeFitter/LayoutGroup
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            
            float nodeWidth = rt.rect.width;
            
            // If width is suspiciously small (e.g., 0), fallback to manual calc
            if(nodeWidth < 10f) nodeWidth = Mathf.Max(100f, current.Keys.Count * nodeDataWidth);

            rt.localPosition = new Vector3(currentX + (nodeWidth / 2f), -depth * levelHeight, 0);
            
            currentX += nodeWidth + nodePadding; // Move X for next leaf
            current = current.Next;
        }
    }

    private void LayoutInternalNodes(BPlusTreeNode<int, string> node)
    {
        if (node.IsLeaf) return;

        // Process children first (Post-order)
        foreach (var child in node.Children)
        {
            LayoutInternalNodes(child);
        }

        if (!_nodeMap.ContainsKey(node)) return;

        RectTransform myRt = _nodeMap[node];
        int depth = GetDepth(node);

        if (node.Children.Count == 0)
        {
            // If an internal node has no children (e.g., due to merging), 
            // just place it at X=0. CenterAndFitTree will handle the rest.
            myRt.localPosition = new Vector3(0, -depth * levelHeight, 0);
            return;
        }

        // Ensure critical children are present
        if (!_nodeMap.ContainsKey(node.Children[0]) || !_nodeMap.ContainsKey(node.Children[node.Children.Count - 1])) return;

        // Now place myself based on the children
        RectTransform firstChild = _nodeMap[node.Children[0]];
        RectTransform lastChild = _nodeMap[node.Children[node.Children.Count - 1]];

        float newX = (firstChild.localPosition.x + lastChild.localPosition.x) / 2f;

        myRt.localPosition = new Vector3(newX, -depth * levelHeight, 0);
    }
    
    public int GetDepth(BPlusTreeNode<int, string> node)
    {
        int d = 0;
        while(node.Parent != null)
        {
            d++;
            node = node.Parent;
        }
        return d;
    }

    private void CenterAndFitTree()
    {
        if (_nodeMap.Count == 0 || treeContainer == null) return;
        
        // Reset scale to 1 before calculation
        treeContainer.localScale = Vector3.one;

        // Calculate Tree Bounds
        float leftEdge = float.MaxValue;
        float rightEdge = float.MinValue;
        
        foreach (var rt in _nodeMap.Values)
        {
            float halfWidth = rt.rect.width / 2f;
            float x = rt.localPosition.x;
            
            if (x - halfWidth < leftEdge) leftEdge = x - halfWidth;
            if (x + halfWidth > rightEdge) rightEdge = x + halfWidth;
        }
        
        // Center alignment
        float mid = (leftEdge + rightEdge) / 2f;
        foreach (var rt in _nodeMap.Values)
        {
            rt.localPosition -= new Vector3(mid, 0, 0);
        }

        // Auto Scaling Logic
        float treeWidth = rightEdge - leftEdge;
        
        // Determine available width from Parent
        RectTransform parentRect = treeContainer.parent as RectTransform;
        if (parentRect != null)
        {
            float availableWidth = parentRect.rect.width;
            
            // Add padding
            float padding = 100f; 
            float maxAllowedWidth = availableWidth - padding;

            if (maxAllowedWidth > 0 && treeWidth > maxAllowedWidth)
            {
                float scaleFactor = maxAllowedWidth / treeWidth;
                // Clamp scale to not be too tiny
                scaleFactor = Mathf.Max(scaleFactor, 0.4f); 
                
                treeContainer.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
            }
        }
    }
    
    private void DrawConnectionsRecursive(BPlusTreeNode<int, string> node)
    {
        if (node.IsLeaf || node.Children == null) return;
        if (!_nodeMap.ContainsKey(node)) return;

        RectTransform parentRect = _nodeMap[node];
        BPlusTreeVisualNode visualNode = parentRect.GetComponent<BPlusTreeVisualNode>();
        
        int numKeys = node.Keys.Count;
        // Rules: i keys -> i+1 lines. 0 keys -> 0 lines.
        int connectionLimit = (numKeys == 0) ? 0 : numKeys + 1;

        // Iterate through ALL children to ensure recursive drawing continues 
        // regardless of whether a connection line is drawn from this node to them.
        for (int i = 0; i < node.Children.Count; i++)
        {
            var child = node.Children[i];
            if (_nodeMap.ContainsKey(child))
            {
                // Only draw a connection if within the limit based on key count
                if (i < connectionLimit)
                {
                    RectTransform childRect = _nodeMap[child];
                    float xOffset = GetChildConnectionXOffset(visualNode, i);
                    CreateConnection(parentRect, childRect, xOffset);
                }
                
                // Always recurse to child
                DrawConnectionsRecursive(child);
            }
        }
    }

    private float GetChildConnectionXOffset(BPlusTreeVisualNode visualNode, int childIndex)
    {
        if (visualNode.SpawnedKeys.Count == 0) return 0f;
        
        // Ensure layout is up to date (might be redundant but safe)
        // LayoutRebuilder.ForceRebuildLayoutImmediate(visualNode.GetComponent<RectTransform>());

        RectTransform nodeRect = visualNode.GetComponent<RectTransform>();
        
        if (childIndex == 0)
        {
            // Left of first key
            RectTransform keyRect = visualNode.SpawnedKeys[0].GetComponent<RectTransform>();
            // Use world position to handle nested hierarchy correctly
            Vector3 worldPos = keyRect.position; 
            Vector3 localPos = nodeRect.InverseTransformPoint(worldPos);
            return localPos.x - (keyRect.rect.width / 2f);
        }
        else if (childIndex >= visualNode.SpawnedKeys.Count) 
        {
            // Right of last key
            RectTransform keyRect = visualNode.SpawnedKeys[visualNode.SpawnedKeys.Count - 1].GetComponent<RectTransform>();
            Vector3 worldPos = keyRect.position; 
            Vector3 localPos = nodeRect.InverseTransformPoint(worldPos);
            return localPos.x + (keyRect.rect.width / 2f);
        }
        else
        {
            // Between keys [i-1] and [i]
            RectTransform leftKey = visualNode.SpawnedKeys[childIndex - 1].GetComponent<RectTransform>();
            RectTransform rightKey = visualNode.SpawnedKeys[childIndex].GetComponent<RectTransform>();
            
            float leftX = nodeRect.InverseTransformPoint(leftKey.position).x + (leftKey.rect.width / 2f);
            float rightX = nodeRect.InverseTransformPoint(rightKey.position).x - (rightKey.rect.width / 2f);
            
            return (leftX + rightX) / 2f;
        }
    }

    private void CreateConnection(RectTransform parent, RectTransform child, float xOffset)
    {
        if (linkLinePrefab == null) return;

        GameObject lineObj = Instantiate(linkLinePrefab, treeContainer);
        // Ensure line is behind nodes
        lineObj.transform.SetAsFirstSibling(); 
        
        RectTransform lineRect = lineObj.GetComponent<RectTransform>();
        
        // Calculate dynamic start/end points relative to container
        // Start: Parent Bottom-Center + Offset
        // End: Child Top-Center
        
        Vector3 startPos = parent.localPosition + new Vector3(xOffset, -parent.rect.height / 2f, 0);
        Vector3 endPos = child.localPosition + new Vector3(0, child.rect.height / 2f, 0);
        
        Vector3 direction = endPos - startPos;
        float distance = direction.magnitude;
        
        // Set Position (Start Point)
        lineRect.localPosition = startPos;
        
        // Set Rotation
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        lineRect.localRotation = Quaternion.Euler(0, 0, angle);
        
        // Set Length (Width of the image)
        lineRect.sizeDelta = new Vector2(distance, 3f); // 3f is line thickness
    }
}