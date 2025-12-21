using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartUIManager : MonoBehaviour
{
    public PlayerHealth playerHealth;
    
    [Header("UI References")]
    public List<Image> heartImages;
    
    [Header("Sprites")]
    public Sprite fullHeart;
    public Sprite emptyHeart;

    void Start()
    {
        if (playerHealth == null)
        {
            // Try to find PlayerHealth if not assigned
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerHealth = player.GetComponent<PlayerHealth>();
            }
        }

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.AddListener(UpdateHearts);
            // Initialize
            UpdateHearts(playerHealth.currentHearts);
        }
        else
        {
            Debug.LogWarning("HeartUIManager: PlayerHealth not found!");
        }
    }

    public void UpdateHearts(int currentHealth)
    {
        for (int i = 0; i < heartImages.Count; i++)
        {
            if (i < currentHealth)
            {
                if (fullHeart != null) heartImages[i].sprite = fullHeart;
                heartImages[i].enabled = true; // Ensure it's visible
            }
            else
            {
                if (emptyHeart != null) 
                {
                    heartImages[i].sprite = emptyHeart;
                }
                else
                {
                    // ...
                }
            }
        }
    }
}