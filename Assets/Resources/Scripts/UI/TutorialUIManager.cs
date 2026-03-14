using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TutorialUIManager : MonoBehaviour
{
    public static TutorialUIManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI contentText;
    public TextMeshProUGUI pageIndicatorText;
    
    [Header("Buttons")]
    public GameObject nextButton;
    public GameObject prevButton;

    private List<TutorialPage> currentPages = new List<TutorialPage>();
    private int currentPageIndex = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (tutorialPanel != null && tutorialPanel.activeInHierarchy)
        {
            // Allow keyboard navigation to change pages
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                NextPage();
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                PrevPage();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseTutorial();
            }
        }
    }

    public void OpenTutorial(List<TutorialPage> pages)
    {
        if (pages == null || pages.Count == 0) return;

        currentPages = pages;
        currentPageIndex = 0;
        
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            UpdatePageDisplay();
        }
    }

    public void CloseTutorial()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }

    public void NextPage()
    {
        if (currentPageIndex < currentPages.Count - 1)
        {
            currentPageIndex++;
            UpdatePageDisplay();
        }
    }

    public void PrevPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            UpdatePageDisplay();
        }
    }

    private void UpdatePageDisplay()
    {
        if (currentPageIndex >= 0 && currentPageIndex < currentPages.Count)
        {
            TutorialPage page = currentPages[currentPageIndex];
            
            if (titleText != null) titleText.text = page.title;
            if (contentText != null) contentText.text = page.content;
            
            if (pageIndicatorText != null)
            {
                pageIndicatorText.text = $"Page {currentPageIndex + 1} / {currentPages.Count}";
            }

            // Button visibility logic: Prev only on page > 0, Next only on page < count - 1
            if (prevButton != null) 
            {
                prevButton.SetActive(currentPageIndex > 0);
            }
            
            if (nextButton != null) 
            {
                nextButton.SetActive(currentPageIndex < currentPages.Count - 1);
            }
        }
    }
}

[System.Serializable]
public class TutorialPage
{
    public string title;
    [TextArea(5, 15)]
    public string content;
}