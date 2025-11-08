using System;
using System.Collections.Generic;
using UnityEngine;

// Represents a node in the B+ tree structure.
[Serializable]
public class BPlusTreeNode<TKey, TValue> where TKey : IComparable<TKey>
{
    // Keys stored in this node
    public List<TKey> Keys { get; set; }

    // Values stored in this node (only used in leaf nodes)
    public List<TValue> Values { get; set; }

    // Child nodes (only used in internal nodes)
    public List<BPlusTreeNode<TKey, TValue>> Children { get; set; }

    // Pointer to the next leaf node (only used in leaf nodes for range queries)
    public BPlusTreeNode<TKey, TValue> Next { get; set; }

    // Indicates whether this node is a leaf node
    public bool IsLeaf { get; set; }

    // Parent node reference
    public BPlusTreeNode<TKey, TValue> Parent { get; set; }

    // Constructor for creating a new node
    public BPlusTreeNode(bool isLeaf)
    {
        Keys = new List<TKey>();
        IsLeaf = isLeaf;

        if (isLeaf)
        {
            Values = new List<TValue>();
            Next = null;
        }
        else
        {
            Children = new List<BPlusTreeNode<TKey, TValue>>();
        }

        Parent = null;
    }

    // Gets the number of keys in this node
    public int KeyCount
    {
        get { return Keys.Count; }
    }

    // Checks if the node is full based on the order
    public bool IsFull(int order)
    {
        return Keys.Count >= order - 1;
    }

    // Checks if the node has minimum keys based on the order
    public bool HasMinimumKeys(int order)
    {
        int minKeys = (int)Math.Ceiling((order - 1) / 2.0);
        return Keys.Count >= minKeys;
    }
}

// B+ Tree implementation
[Serializable]
public class BPlusTree<TKey, TValue> where TKey : IComparable<TKey>
{
    // The order of the B+ tree
    public int Order { get; private set; }

    // Root node of the tree
    public BPlusTreeNode<TKey, TValue> Root { get; private set; }

    // Reference to the first leaf node (for sequential access)
    public BPlusTreeNode<TKey, TValue> FirstLeaf { get; private set; }

    // Total number of key-value pairs in the tree
    public int Count { get; private set; }

    // Constructor
    public BPlusTree(int order)
    {
        if (order < 3)
        {
            // Set the B+ Tree order at least 3
            order = 3;
        }

        Order = order;
        Root = new BPlusTreeNode<TKey, TValue>(true);
        FirstLeaf = Root;
        Count = 0;
    }

    #region Insertion

    // Inserts a key-value pair into the B+ tree
    public void Insert(TKey key, TValue value)
    {
        if (key == null)
        {
            // Return if inserting a null key into the B+ Tree
            return;
        }

        BPlusTreeNode<TKey, TValue> leaf = FindLeafNode(Root, key);

        // Insert into leaf node
        bool inserted = InsertIntoLeaf(leaf, key, value);

        if (inserted)
        {
            // Check if leaf is full and needs to split
            if (leaf.IsFull(Order))
            {
                SplitLeafNode(leaf);
            }

            Count++;
        }
    }

    // Inserts a key-value pair into a leaf node at the correct position
    private bool InsertIntoLeaf(BPlusTreeNode<TKey, TValue> leaf, TKey key, TValue value)
    {
        int insertPos = 0;
        while (insertPos < leaf.Keys.Count && key.CompareTo(leaf.Keys[insertPos]) > 0)
        {
            insertPos++;
        }

        // Check for duplicate keys
        if (insertPos < leaf.Keys.Count && key.CompareTo(leaf.Keys[insertPos]) == 0)
        {
            return false;
        }

        leaf.Keys.Insert(insertPos, key);
        leaf.Values.Insert(insertPos, value);
        return true;
    }

    // Splits a leaf node when it becomes full
    private void SplitLeafNode(BPlusTreeNode<TKey, TValue> leaf)
    {
        int midPoint = Order / 2;

        // Create new leaf node
        BPlusTreeNode<TKey, TValue> newLeaf = new BPlusTreeNode<TKey, TValue>(true);

        // Move keys from midPoint onward to the new leaf
        newLeaf.Keys.AddRange(leaf.Keys.GetRange(midPoint, leaf.Keys.Count - midPoint));
        newLeaf.Values.AddRange(leaf.Values.GetRange(midPoint, leaf.Values.Count - midPoint));

        // Remove moved keys and values from original leaf
        leaf.Keys.RemoveRange(midPoint, leaf.Keys.Count - midPoint);
        leaf.Values.RemoveRange(midPoint, leaf.Values.Count - midPoint);

        // Update leaf pointers
        newLeaf.Next = leaf.Next;
        leaf.Next = newLeaf;

        // Promote the first key of the right node to parent
        TKey keyToPromote = newLeaf.Keys[0];

        // Insert into parent
        if (leaf == Root)
        {
            // Create new root
            BPlusTreeNode<TKey, TValue> newRoot = new BPlusTreeNode<TKey, TValue>(false);
            newRoot.Keys.Add(keyToPromote);
            newRoot.Children.Add(leaf);
            newRoot.Children.Add(newLeaf);

            leaf.Parent = newRoot;
            newLeaf.Parent = newRoot;

            Root = newRoot;
        }
        else
        {
            InsertIntoParent(leaf.Parent, keyToPromote, newLeaf);
        }
    }

    // Inserts a key and child pointer into an internal node
    private void InsertIntoParent(BPlusTreeNode<TKey, TValue> parent, TKey key, BPlusTreeNode<TKey, TValue> newChild)
    {
        int insertPos = 0;
        while (insertPos < parent.Keys.Count && key.CompareTo(parent.Keys[insertPos]) > 0)
        {
            insertPos++;
        }

        parent.Keys.Insert(insertPos, key);
        parent.Children.Insert(insertPos + 1, newChild);
        newChild.Parent = parent;

        // Check if parent needs to split
        if (parent.IsFull(Order))
        {
            SplitInternalNode(parent);
        }
    }

    // Splits an internal node when it becomes full
    private void SplitInternalNode(BPlusTreeNode<TKey, TValue> node)
    {
        int midPoint = Order / 2;

        // Create new internal node
        BPlusTreeNode<TKey, TValue> newNode = new BPlusTreeNode<TKey, TValue>(false);

        // Promote key at midPoint to parent
        TKey keyToPromote = node.Keys[midPoint];

        // Move keys after midPoint to new node
        newNode.Keys.AddRange(node.Keys.GetRange(midPoint + 1, node.Keys.Count - midPoint - 1));
        newNode.Children.AddRange(node.Children.GetRange(midPoint + 1, node.Children.Count - midPoint - 1));

        // Update parent pointers for moved children
        foreach (var child in newNode.Children)
        {
            child.Parent = newNode;
        }

        // Remove moved keys and children from original node
        node.Keys.RemoveRange(midPoint, node.Keys.Count - midPoint);
        node.Children.RemoveRange(midPoint + 1, node.Children.Count - midPoint - 1);

        // Insert into parent
        if (node == Root)
        {
            // Create new root
            BPlusTreeNode<TKey, TValue> newRoot = new BPlusTreeNode<TKey, TValue>(false);
            newRoot.Keys.Add(keyToPromote);
            newRoot.Children.Add(node);
            newRoot.Children.Add(newNode);

            node.Parent = newRoot;
            newNode.Parent = newRoot;

            Root = newRoot;
        }
        else
        {
            InsertIntoParent(node.Parent, keyToPromote, newNode);
        }
    }

    #endregion

    #region Search

    // Searches for a value by key
    public bool Search(TKey key, out TValue value)
    {
        value = default(TValue);

        if (key == null)
        {
            return false;
        }

        BPlusTreeNode<TKey, TValue> leaf = FindLeafNode(Root, key);

        int index = leaf.Keys.BinarySearch(key);
        if (index >= 0)
        {
            value = leaf.Values[index];
            return true;
        }

        return false;
    }

    // Finds the leaf node that should contain the given key
    private BPlusTreeNode<TKey, TValue> FindLeafNode(BPlusTreeNode<TKey, TValue> node, TKey key)
    {
        if (node.IsLeaf)
        {
            return node;
        }

        int i = 0;
        while (i < node.Keys.Count && key.CompareTo(node.Keys[i]) >= 0)
        {
            i++;
        }

        return FindLeafNode(node.Children[i], key);
    }

    // Returns the sequence of child indices taken from the root down to the leaf when searching for the specified target
    public int[] GetSearchIndices(TKey key)
    {
        if (key == null || Root == null)
        {
            return new int[0];
        }

        List<int> indices = new List<int>();
        BPlusTreeNode<TKey, TValue> node = Root;
        while (node != null && !node.IsLeaf)
        {
            int i = 0;
            while (i < node.Keys.Count && key.CompareTo(node.Keys[i]) >= 0)
            {
                i++;
            }

            indices.Add(i);
            node = node.Children[i];
        }

        return indices.ToArray();
    }

    // Performs a range query to get all key-value pairs between minKey and maxKey
    public List<KeyValuePair<TKey, TValue>> RangeSearch(TKey minKey, TKey maxKey)
    {
        List<KeyValuePair<TKey, TValue>> results = new List<KeyValuePair<TKey, TValue>>();

        if (minKey == null || maxKey == null)
        {
            return results;
        }

        if (minKey.CompareTo(maxKey) > 0)
        {
            return results;
        }

        // Find the leaf containing minKey
        BPlusTreeNode<TKey, TValue> leaf = FindLeafNode(Root, minKey);

        // Traverse leaf nodes and collect values in range
        while (leaf != null)
        {
            for (int i = 0; i < leaf.Keys.Count; i++)
            {
                TKey key = leaf.Keys[i];
                if (key.CompareTo(minKey) >= 0 && key.CompareTo(maxKey) <= 0)
                {
                    results.Add(new KeyValuePair<TKey, TValue>(key, leaf.Values[i]));
                }
                else if (key.CompareTo(maxKey) > 0)
                {
                    return results; // No more keys in range
                }
            }

            leaf = leaf.Next; // Move to next leaf
        }

        return results;
    }

    #endregion

    #region Deletion

    // Deletes a key-value pair from the tree
    public bool Delete(TKey key)
    {
        if (key == null)
        {
            return false;
        }

        BPlusTreeNode<TKey, TValue> leaf = FindLeafNode(Root, key);
        int index = leaf.Keys.BinarySearch(key);

        if (index < 0)
        {
            return false;
        }

        TKey deletedKey = leaf.Keys[index];

        // Remove from leaf
        leaf.Keys.RemoveAt(index);
        leaf.Values.RemoveAt(index);
        Count--;

        RestructureAfterDeletion(leaf, deletedKey);

        return true;
    }

    // Handles all restructuring after a key deletion
    private void RestructureAfterDeletion(BPlusTreeNode<TKey, TValue> leaf, TKey deletedKey)
    {
        // Check if tree is now empty
        if (leaf == Root && leaf.Keys.Count == 0)
        {
            Root = new BPlusTreeNode<TKey, TValue>(true);
            FirstLeaf = Root;
            return;
        }

        // Handle leaf underflow if necessary
        int minKeys = (int)Math.Ceiling(Order / 2.0) - 1;
        if (leaf != Root && leaf.Keys.Count < minKeys)
        {
            HandleUnderflow(leaf);
        }

        // Update internal keys if necessary
        UpdateInternalKey(Root, deletedKey);
        
        // After updating internal keys, check for any internal node underflows
        CheckInternalUnderflows(Root, minKeys);
    }
    
    // Recursively checks and handles underflows in internal nodes
    private void CheckInternalUnderflows(BPlusTreeNode<TKey, TValue> node, int minKeys)
    {
        if (node == null || node.IsLeaf)
        {
            return;
        }
        
        // Check children for underflow (from right to left to avoid index issues)
        for (int i = node.Children.Count - 1; i >= 0; i--)
        {
            BPlusTreeNode<TKey, TValue> child = node.Children[i];
            
            if (child != Root && !child.IsLeaf && child.Keys.Count < minKeys)
            {
                HandleUnderflow(child);
            }
            
            // Recursively check grandchildren
            CheckInternalUnderflows(child, minKeys);
        }
    }

    // Updates or removes a key in internal nodes after deletion from leaf
    private bool UpdateInternalKey(BPlusTreeNode<TKey, TValue> node, TKey keyToRemove)
    {
        if (node == null || node.IsLeaf)
        {
            return false;
        }

        // Find the key if present in this node
        int keyIndex = node.Keys.IndexOf(keyToRemove);
        if (keyIndex >= 0)
        {
            BPlusTreeNode<TKey, TValue> rightChild = node.Children[keyIndex + 1];
            
            // Try to find the smallest key in right child's subtree as the replacement key
            TKey replacementKey = FindSmallestKey(rightChild);
            
            if (replacementKey != null)
            {
                // Replace the deleted key with the new separator
                node.Keys[keyIndex] = replacementKey;
            }
            else
            {
                // No valid replacement - merge the children
                BPlusTreeNode<TKey, TValue> leftChild = node.Children[keyIndex];
                MergeWithRight(leftChild, rightChild, node, keyIndex);
            }
            
            return true; // Found and handled, stop searching
        }

        // Recursively search children until found
        foreach (var child in node.Children)
        {
            if (UpdateInternalKey(child, keyToRemove))
            {
                return true; // Found in subtree, stop searching
            }
        }

        return false; // Not found in this subtree
    }

    // Finds the smallest key in a subtree
    private TKey FindSmallestKey(BPlusTreeNode<TKey, TValue> node)
    {
        if (node == null)
        {
            return default(TKey);
        }

        // Get the first key
        while (!node.IsLeaf)
        {
            node = node.Children[0];
        }

        // Return first key in the leaf
        return node.Keys.Count > 0 ? node.Keys[0] : default(TKey);
    }

    // Handles underflow in a node after deletion
    private void HandleUnderflow(BPlusTreeNode<TKey, TValue> node)
    {
        BPlusTreeNode<TKey, TValue> parent = node.Parent;
        if (parent == null) return;

        int nodeIndex = parent.Children.IndexOf(node);
        
        int minKeys = (int)Math.Ceiling(Order / 2.0) - 1;

        // Try to borrow from right sibling first (right-bias approach)
        if (nodeIndex < parent.Children.Count - 1)
        {
            BPlusTreeNode<TKey, TValue> rightSibling = parent.Children[nodeIndex + 1];
            if (rightSibling.Keys.Count > minKeys)
            {
                BorrowFromRight(node, rightSibling, parent, nodeIndex);
                return;
            }
        }

        // Try to borrow from left sibling
        if (nodeIndex > 0)
        {
            BPlusTreeNode<TKey, TValue> leftSibling = parent.Children[nodeIndex - 1];
            if (leftSibling.Keys.Count > minKeys)
            {
                BorrowFromLeft(node, leftSibling, parent, nodeIndex);
                return;
            }
        }

        // Merge with sibling (prefer right for consistency with right-bias)
        if (nodeIndex < parent.Children.Count - 1)
        {
            MergeWithRight(node, parent.Children[nodeIndex + 1], parent, nodeIndex);
        }
        else
        {
            MergeWithLeft(node, parent.Children[nodeIndex - 1], parent, nodeIndex);
        }
    }

    // Borrows a key from the left sibling
    private void BorrowFromLeft(BPlusTreeNode<TKey, TValue> node, BPlusTreeNode<TKey, TValue> leftSibling,
        BPlusTreeNode<TKey, TValue> parent, int nodeIndex)
    {
        if (node.IsLeaf)
        {
            // Move last key-value from left sibling to first position of node
            node.Keys.Insert(0, leftSibling.Keys[leftSibling.Keys.Count - 1]);
            node.Values.Insert(0, leftSibling.Values[leftSibling.Values.Count - 1]);

            leftSibling.Keys.RemoveAt(leftSibling.Keys.Count - 1);
            leftSibling.Values.RemoveAt(leftSibling.Values.Count - 1);

            // Update parent key
            parent.Keys[nodeIndex - 1] = node.Keys[0];
        }
        else
        {
            // Internal node borrow
            node.Keys.Insert(0, parent.Keys[nodeIndex - 1]);
            parent.Keys[nodeIndex - 1] = leftSibling.Keys[leftSibling.Keys.Count - 1];
            leftSibling.Keys.RemoveAt(leftSibling.Keys.Count - 1);

            // Move child pointer
            BPlusTreeNode<TKey, TValue> childToMove = leftSibling.Children[leftSibling.Children.Count - 1];
            leftSibling.Children.RemoveAt(leftSibling.Children.Count - 1);
            node.Children.Insert(0, childToMove);
            childToMove.Parent = node;
        }
    }

    // Borrows a key from the right sibling
    private void BorrowFromRight(BPlusTreeNode<TKey, TValue> node, BPlusTreeNode<TKey, TValue> rightSibling,
        BPlusTreeNode<TKey, TValue> parent, int nodeIndex)
    {
        if (node.IsLeaf)
        {
            // Move first key-value from right sibling to last position of node
            node.Keys.Add(rightSibling.Keys[0]);
            node.Values.Add(rightSibling.Values[0]);

            rightSibling.Keys.RemoveAt(0);
            rightSibling.Values.RemoveAt(0);

            // Update parent key
            parent.Keys[nodeIndex] = rightSibling.Keys[0];
        }
        else
        {
            // Internal node borrow
            node.Keys.Add(parent.Keys[nodeIndex]);
            parent.Keys[nodeIndex] = rightSibling.Keys[0];
            rightSibling.Keys.RemoveAt(0);

            // Move child pointer
            BPlusTreeNode<TKey, TValue> childToMove = rightSibling.Children[0];
            rightSibling.Children.RemoveAt(0);
            node.Children.Add(childToMove);
            childToMove.Parent = node;
        }
    }

    // Merges node with its left sibling
    private void MergeWithLeft(BPlusTreeNode<TKey, TValue> node, BPlusTreeNode<TKey, TValue> leftSibling,
        BPlusTreeNode<TKey, TValue> parent, int nodeIndex)
    {
        if (node.IsLeaf)
        {
            // Merge all keys and values
            leftSibling.Keys.AddRange(node.Keys);
            leftSibling.Values.AddRange(node.Values);
            leftSibling.Next = node.Next;
        }
        else
        {
            // Merge internal nodes
            leftSibling.Keys.Add(parent.Keys[nodeIndex - 1]);
            leftSibling.Keys.AddRange(node.Keys);
            leftSibling.Children.AddRange(node.Children);

            // Update parent pointers
            foreach (var child in node.Children)
            {
                child.Parent = leftSibling;
            }
        }

        // Remove key and child pointer from parent
        parent.Keys.RemoveAt(nodeIndex - 1);
        parent.Children.RemoveAt(nodeIndex);

        // Handle parent underflow
        if (parent == Root && parent.Keys.Count == 0)
        {
            Root = leftSibling;
            Root.Parent = null;
            if (Root.IsLeaf)
            {
                FirstLeaf = Root;
            }
        }
        else if (parent != Root)
        {
            int minKeys = (int)Math.Ceiling(Order / 2.0) - 1;
            if (parent.Keys.Count < minKeys)
            {
                HandleUnderflow(parent);
            }
        }
    }

    // Merges node with its right sibling
    private void MergeWithRight(BPlusTreeNode<TKey, TValue> node, BPlusTreeNode<TKey, TValue> rightSibling,
        BPlusTreeNode<TKey, TValue> parent, int nodeIndex)
    {
        if (node.IsLeaf)
        {
            // Merge all keys and values
            node.Keys.AddRange(rightSibling.Keys);
            node.Values.AddRange(rightSibling.Values);
            node.Next = rightSibling.Next;
        }
        else
        {
            // Merge internal nodes
            node.Keys.Add(parent.Keys[nodeIndex]);
            node.Keys.AddRange(rightSibling.Keys);
            node.Children.AddRange(rightSibling.Children);

            // Update parent pointers
            foreach (var child in rightSibling.Children)
            {
                child.Parent = node;
            }
        }

        // Remove key and child pointer from parent
        parent.Keys.RemoveAt(nodeIndex);
        parent.Children.RemoveAt(nodeIndex + 1);

        // Handle parent underflow
        if (parent == Root && parent.Keys.Count == 0)
        {
            Root = node;
            Root.Parent = null;
            if (Root.IsLeaf)
            {
                FirstLeaf = Root;
            }
        }
        else if (parent != Root)
        {
            int minKeys = (int)Math.Ceiling(Order / 2.0) - 1;
            if (parent.Keys.Count < minKeys)
            {
                HandleUnderflow(parent);
            }
        }
    }

    #endregion

    #region Utility Methods

    // Clears all entries from the tree
    public void Clear()
    {
        Root = new BPlusTreeNode<TKey, TValue>(true);
        FirstLeaf = Root;
        Count = 0;
    }

    // Checks if the tree contains a specific key
    public bool ContainsKey(TKey key)
    {
        return Search(key, out _);
    }

    // Gets all key-value pairs in the tree
    public List<KeyValuePair<TKey, TValue>> GetAllEntries()
    {
        List<KeyValuePair<TKey, TValue>> entries = new List<KeyValuePair<TKey, TValue>>();
        BPlusTreeNode<TKey, TValue> leaf = FirstLeaf;

        while (leaf != null)
        {
            for (int i = 0; i < leaf.Keys.Count; i++)
            {
                entries.Add(new KeyValuePair<TKey, TValue>(leaf.Keys[i], leaf.Values[i]));
            }
            leaf = leaf.Next;
        }

        return entries;
    }

    // Prints the tree structure (for debugging)
    public void PrintTree()
    {
        Debug.Log("=== B+ Tree Structure ===");
        Debug.Log($"Order: {Order}, Count: {Count}");
        PrintNode(Root, 0);
    }

    private void PrintNode(BPlusTreeNode<TKey, TValue> node, int level)
    {
        if (node == null) return;

        string indent = new string(' ', level * 2);
        string nodeType = node.IsLeaf ? "LEAF" : "INTERNAL";
        string keys = string.Join(", ", node.Keys);

        Debug.Log($"{indent}[{nodeType}] Keys: {keys}");

        if (!node.IsLeaf)
        {
            foreach (var child in node.Children)
            {
                PrintNode(child, level + 1);
            }
        }
    }

    // Validates the B+ tree structure
    public bool ValidateTree()
    {
        if (Root == null)
        {
            Debug.LogError("Validation failed: Root is null");
            return false;
        }

        int minKeys = (int)Math.Ceiling(Order / 2.0) - 1;
        int leafLevel = -1;

        return ValidateNode(Root, minKeys, 0, ref leafLevel);
    }

    // Recursively validates a node
    private bool ValidateNode(BPlusTreeNode<TKey, TValue> node, int minKeys, int level, ref int leafLevel)
    {
        // Check key count
        if (node != Root && node.Keys.Count < minKeys)
        {
            Debug.LogError($"Node at level {level} has {node.Keys.Count} keys, below minimum {minKeys}");
            return false;
        }

        if (node.Keys.Count > Order - 1)
        {
            Debug.LogError($"Node at level {level} has {node.Keys.Count} keys, exceeds maximum {Order - 1}");
            return false;
        }

        // Check keys are sorted
        for (int i = 0; i < node.Keys.Count - 1; i++)
        {
            if (node.Keys[i].CompareTo(node.Keys[i + 1]) >= 0)
            {
                Debug.LogError($"Keys not sorted at level {level}");
                return false;
            }
        }

        if (node.IsLeaf)
        {
            // Check all leaves at same level
            if (leafLevel == -1)
            {
                leafLevel = level;
            }
            else if (leafLevel != level)
            {
                Debug.LogError($"Leaves at different levels: {leafLevel} vs {level}");
                return false;
            }

            // Check values count
            if (node.Values.Count != node.Keys.Count)
            {
                Debug.LogError($"Leaf has {node.Keys.Count} keys but {node.Values.Count} values");
                return false;
            }
        }
        else
        {
            // Check children count
            if (node.Children.Count != node.Keys.Count + 1)
            {
                Debug.LogError($"Internal node at level {level} has {node.Keys.Count} keys but {node.Children.Count} children");
                return false;
            }

            // Check parent pointers and validate children
            foreach (var child in node.Children)
            {
                if (child.Parent != node)
                {
                    Debug.LogError($"Child has incorrect parent pointer at level {level}");
                    return false;
                }

                if (!ValidateNode(child, minKeys, level + 1, ref leafLevel))
                {
                    return false;
                }
            }
        }

        return true;
    }

    #endregion
}