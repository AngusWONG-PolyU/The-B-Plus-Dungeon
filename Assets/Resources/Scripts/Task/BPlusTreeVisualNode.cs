using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BPlusTreeVisualNode : MonoBehaviour
{
    [Header("References")]
    public Transform keyContainer;
    public GameObject keyPrefab;
    
    // Data
    public BPlusTreeNode<int, string> CoreNode { get; private set; }
    public List<GameObject> SpawnedKeys { get; private set; } = new List<GameObject>();

    public void Initialize(BPlusTreeNode<int, string> node)
    {
        CoreNode = node;
        RenderKeys();
    }

    public void RenderKeys()
    {
        // Clear old keys
        foreach (Transform child in keyContainer)
        {
            Destroy(child.gameObject);
        }
        SpawnedKeys.Clear();

        // Spawn new keys
        foreach (var key in CoreNode.Keys)
        {
            GameObject k = Instantiate(keyPrefab, keyContainer);
            
            TextMeshProUGUI t = k.GetComponentInChildren<TextMeshProUGUI>();
            if(t) t.text = key.ToString();

            // Ensure square shape via LayoutElement if not present
            LayoutElement le = k.GetComponent<LayoutElement>();
            if (le == null) le = k.AddComponent<LayoutElement>();
            
            // Set a default square size (e.g. 50x50)
            if (le.preferredWidth <= 0) le.preferredWidth = 50f;
            if (le.preferredHeight <= 0) le.preferredHeight = 50f; // Make it square
            le.minWidth = 50f;
            le.minHeight = 50f;
            
            SpawnedKeys.Add(k);
        }
    }
}