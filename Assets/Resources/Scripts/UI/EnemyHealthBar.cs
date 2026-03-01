using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyHealthBar : MonoBehaviour
{
    public static EnemyHealthBar Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image enemyIcon;
    [SerializeField] private TextMeshProUGUI enemyNameText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Hide on start
        Hide();
    }

    public void Setup(string enemyName, Sprite icon, int currentHealth, int maxHealth)
    {
        if (uiPanel != null)
        {
            uiPanel.SetActive(true);
        }

        if (enemyNameText != null)
        {
            enemyNameText.text = enemyName;
        }

        if (enemyIcon != null && icon != null)
        {
            enemyIcon.sprite = icon;
            enemyIcon.gameObject.SetActive(true);
        }
        else if (enemyIcon != null)
        {
            enemyIcon.gameObject.SetActive(false);
        }

        UpdateHealthBar(currentHealth, maxHealth);
    }

    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (healthBarFill != null)
        {
            float fillAmount = (float)currentHealth / maxHealth;
            healthBarFill.fillAmount = Mathf.Clamp01(fillAmount);
        }
    }

    public void Hide()
    {
        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
        }
    }
}