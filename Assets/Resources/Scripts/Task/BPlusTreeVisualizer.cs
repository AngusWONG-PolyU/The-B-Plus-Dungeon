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
    public float nodePadding = 40f;   // Gap between nodes

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
        
        // 4. Center the whole tree in the container
        CenterTree();

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
        // Use the linked list property of B+ Tree leaves!
        BPlusTreeNode<int, string> current = tree.FirstLeaf;
        float currentX = 0f;

        while (current != null)
        {
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

        // Now place myself based on children
        RectTransform myRt = _nodeMap[node];
        RectTransform firstChild = _nodeMap[node.Children[0]];
        RectTransform lastChild = _nodeMap[node.Children[node.Children.Count - 1]];

        float newX = (firstChild.localPosition.x + lastChild.localPosition.x) / 2f;
        int depth = GetDepth(node);

        myRt.localPosition = new Vector3(newX, -depth * levelHeight, 0);
    }
    
    private int GetDepth(BPlusTreeNode<int, string> node)
    {
        int d = 0;
        while(node.Parent != null)
        {
            d++;
            node = node.Parent;
        }
        return d;
    }

    private void CenterTree()
    {
        if (_nodeMap.Count == 0) return;
        
        // Calculate Bounds
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        foreach (var rt in _nodeMap.Values)
        {
            if (rt.localPosition.x < minX) minX = rt.localPosition.x;
            if (rt.localPosition.x > maxX) maxX = rt.localPosition.x;
        }
        
        float mid = (minX + maxX) / 2f;
        
        foreach (var rt in _nodeMap.Values)
        {
            rt.localPosition -= new Vector3(mid, 0, 0);
        }
    }
    
    private void DrawConnectionsRecursive(BPlusTreeNode<int, string> node)
    {
        if (node.IsLeaf || node.Children == null) return;

        RectTransform parentRect = _nodeMap[node];
        
        foreach (var child in node.Children)
        {
            RectTransform childRect = _nodeMap[child];
            if (childRect != null)
            {
                CreateConnection(parentRect, childRect);
                DrawConnectionsRecursive(child);
            }
        }
    }

    private void CreateConnection(RectTransform parent, RectTransform child)
    {
        if (linkLinePrefab == null) return;

        GameObject lineObj = Instantiate(linkLinePrefab, treeContainer);
        // Ensure line is behind nodes
        lineObj.transform.SetAsFirstSibling(); 
        
        RectTransform lineRect = lineObj.GetComponent<RectTransform>();
        
        // Calculate dynamic start/end points relative to container
        // Start: Parent Bottom-Center
        // End: Child Top-Center
        
        Vector3 startPos = parent.localPosition + new Vector3(0, -parent.rect.height / 2f, 0);
        Vector3 endPos = child.localPosition + new Vector3(0, child.rect.height / 2f, 0);
        
        Vector3 direction = endPos - startPos;
        float distance = direction.magnitude;
        
        // Set Position (Start Point)
        lineRect.localPosition = startPos;
        
        // Set Rotation
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        lineRect.localRotation = Quaternion.Euler(0, 0, angle);
        
        // Set Length (Width of the image)
        lineRect.sizeDelta = new Vector2(distance, 2f); // 2f is line thickness
    }
}