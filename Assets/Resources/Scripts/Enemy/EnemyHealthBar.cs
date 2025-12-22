using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Canvas canvas;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        
        if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
        {
            canvas.renderMode = RenderMode.WorldSpace;
        }
    }

    private void LateUpdate()
    {
        if (canvas != null && mainCamera != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
        }
    }

    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (healthBarFill != null)
        {
            float fillAmount = (float)currentHealth / maxHealth;
            healthBarFill.fillAmount = Mathf.Clamp01(fillAmount);
        }
    }
    
    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }
}