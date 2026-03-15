using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PerformanceManager : MonoBehaviour
{
    public static PerformanceManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject performancePanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI avgInsertTimeText;
    public TextMeshProUGUI insertMistakesText;
    public TextMeshProUGUI avgDeleteTimeText;
    public TextMeshProUGUI deleteMistakesText;
    public TextMeshProUGUI searchMistakesText;
    public TextMeshProUGUI totalScoreText;
    public TextMeshProUGUI gradeText;
    public Button closeButton;

    // Data tracking
    private List<float> _insertTimes = new List<float>();
    private List<float> _deleteTimes = new List<float>();
    private int _insertMistakes;
    private int _deleteMistakes;
    private int _searchMistakes;

    private float _currentTaskStartTime;

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

        if (performancePanel != null)
        {
            performancePanel.SetActive(false);
        }
        
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
    }

    public void StartTaskTimer()
    {
        _currentTaskStartTime = Time.time;
    }

    public void RecordTaskResult(BPlusTreeTaskType type, bool success)
    {
        float timeTaken = Time.time - _currentTaskStartTime;

        if (type == BPlusTreeTaskType.Insertion)
        {
            if (success) _insertTimes.Add(timeTaken);
            else _insertMistakes++;
        }
        else if (type == BPlusTreeTaskType.Deletion)
        {
            if (success) _deleteTimes.Add(timeTaken);
            else _deleteMistakes++;
        }
    }

    public void RecordSearchMistake()
    {
        _searchMistakes++;
    }

    public void ShowPerformanceUI(bool isVictory, bool isForceExit = false)
    {
        if (performancePanel == null) return;

        Time.timeScale = 0f; // Pause the game
        performancePanel.SetActive(true);

        if (titleText != null)
        {
            if (isForceExit)
            {
                titleText.text = "Exited Dungeon";
                titleText.color = Color.white;
            }
            else
            {
                titleText.text = isVictory ? "Dungeon Cleared!" : "You Died...";
                titleText.color = isVictory ? new Color(1f, 0.84f, 0f) : Color.red; // Gold / Red
            }
        }

        // Calculate Averages
        float avgInsert = CalculateAverage(_insertTimes);
        float avgDelete = CalculateAverage(_deleteTimes);

        // Display Stats
        if (avgInsertTimeText != null) avgInsertTimeText.text = $"Average Insert Time: {(avgInsert > 0 ? $"{avgInsert:F1}s" : "--")} <size=80%><color=#999999>({_insertTimes.Count} Success)</color></size>";
        if (insertMistakesText != null) insertMistakesText.text = "Insert Mistakes: " + _insertMistakes.ToString();
        
        if (avgDeleteTimeText != null) avgDeleteTimeText.text = $"Average Delete Time: {(avgDelete > 0 ? $"{avgDelete:F1}s" : "--")} <size=80%><color=#999999>({_deleteTimes.Count} Success)</color></size>";
        if (deleteMistakesText != null) deleteMistakesText.text = "Delete Mistakes: " + _deleteMistakes.ToString();

        if (searchMistakesText != null) searchMistakesText.text = "Search Mistakes: " + _searchMistakes.ToString();

        // Calculate Score
        int score = CalculateScore(avgInsert, avgDelete, isVictory, isForceExit, out string grade);

        if (totalScoreText != null) totalScoreText.text = "Total Score: " + score.ToString();
        
        if (gradeText != null)
        {
            gradeText.text = grade;
            switch (grade)
            {
                case "S": gradeText.color = new Color(1f, 0.84f, 0f); break; // Gold
                case "A": gradeText.color = new Color(0.8f, 0.2f, 0.2f); break; // Red
                case "B": gradeText.color = new Color(0.2f, 0.5f, 0.8f); break; // Blue
                case "C": gradeText.color = new Color(0.5f, 0.8f, 0.2f); break; // Green
                default: gradeText.color = Color.white; break;
            }
        }
    }

    private float CalculateAverage(List<float> times)
    {
        if (times.Count == 0) return 0f;
        float sum = 0f;
        foreach (float t in times) sum += t;
        return sum / times.Count;
    }

    private int CalculateScore(float avgInsert, float avgDelete, bool isVictory, bool isForceExit, out string grade)
    {
        int score = 0;
        
        // Base score for completes
        score += _insertTimes.Count * 100;
        score += _deleteTimes.Count * 150;

        // Time Bonus (Do not give time bonus if player simply gives up / force exits)
        if (!isForceExit)
        {
            foreach(float t in _insertTimes) score += Mathf.FloorToInt(Mathf.Max(0, 15f - t) * 10);
            foreach(float t in _deleteTimes) score += Mathf.FloorToInt(Mathf.Max(0, 20f - t) * 10);
        }

        // Mistake Penalty
        score -= _insertMistakes * 100;
        score -= _deleteMistakes * 100;
        score -= _searchMistakes * 50;

        int totalMistakes = _insertMistakes + _deleteMistakes + _searchMistakes;
        
        // Only give Perfect Clear bonus if they actually completed the dungeon (isVictory)
        if (isVictory && totalMistakes == 0 && (_insertTimes.Count > 0 || _deleteTimes.Count > 0))
        {
            score += 500; // Perfect clear bonus
        }

        // Clamp to positive
        if (score < 0) score = 0;

        // Calculate Grade
        if (isForceExit) 
        {
            grade = "--"; // No grade for giving up
        }
        else if (score >= 2500 && totalMistakes == 0) grade = "S";
        else if (score >= 1500) grade = "A";
        else if (score >= 800) grade = "B";
        else if (score >= 400) grade = "C";
        else grade = "D";

        return score;
    }

    private void ClosePanel()
    {
        Time.timeScale = 1f;

        // Reset the performance tracking variables
        _insertTimes.Clear();
        _deleteTimes.Clear();
        _insertMistakes = 0;
        _deleteMistakes = 0;
        _searchMistakes = 0;
        
        if (performancePanel != null)
        {
            performancePanel.SetActive(false);
        }
    }
}