using System;
using UnityEngine;

public class BPlusTreeTest : MonoBehaviour
{
    void Start()
    {
        // Create a B+ Tree with int keys and string values
        BPlusTree<int, string> tree = new BPlusTree<int, string>(3);

        Debug.Log("=== Testing B+ Tree ===");

        // Insert values
        Debug.Log("\n--- Inserting: 5, 10, 15, 20, 25, 30, 35 ---");
        tree.Insert(5, "five");
        tree.Insert(10, "ten");
        tree.Insert(15, "fifteen");
        tree.Insert(20, "twenty");
        tree.Insert(25, "twenty-five");
        tree.Insert(30, "thirty");
        tree.Insert(35, "thirty-five");
        
        tree.PrintTree();

        // Search
        Debug.Log("\n--- Searching for 20 ---");
        if (tree.Search(20, out string value))
        {
            Debug.Log($"Found: {value}");
        }

        // Delete
        Debug.Log("\n--- Deleting 20 ---");
        tree.Delete(20);
        tree.PrintTree();
        Debug.Log($"Root node has {tree.Root.Keys.Count} keys");
        Debug.Log($"Root IsLeaf: {tree.Root.IsLeaf}");

        // Validate
        Debug.Log("\n--- Validating tree ---");
        bool isValid = tree.ValidateTree();
        Debug.Log($"Tree is valid: {isValid}");

        // Range search
        Debug.Log("\n--- Range search [10, 30] ---");
        var results = tree.RangeSearch(10, 30);
        foreach (var kvp in results)
        {
            Debug.Log($"Key: {kvp.Key}, Value: {kvp.Value}");
        }
    }
}