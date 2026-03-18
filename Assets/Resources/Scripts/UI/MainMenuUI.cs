using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject mainMenuPanel;

    void Start()
    {
        Time.timeScale = 0f;
        mainMenuPanel.SetActive(true);
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        mainMenuPanel.SetActive(false);
    }
}