using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DoorController : MonoBehaviour, ITaskTrigger
{
    public enum DoorType { Disappearing, Sliding, Rotating }
    
    [Header("Task Interaction")]
    public bool requiresTask = false;
    public bool isTaskLocked = false;

    [Header("Settings")]
    public DoorType type = DoorType.Disappearing;
    public float animationDuration = 1.0f;
    
    [Header("Sliding Settings")]
    public Vector3 slideOffset = new Vector3(0, -3, 0); // Default slide down
    
    [Header("Rotating Settings")]
    public Vector3 rotateAngle = new Vector3(0, 90, 0);
    
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool isOpen = false;
    private NavMeshObstacle obstacle;
    
    void Awake()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        obstacle = GetComponent<NavMeshObstacle>();
    }
    
    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        
        if (obstacle != null) obstacle.enabled = false;

        StopAllCoroutines();
        
        switch (type)
        {
            case DoorType.Disappearing:
                gameObject.SetActive(false);
                break;
                
            case DoorType.Sliding:
                StartCoroutine(MoveTo(initialPosition + slideOffset));
                break;
                
            case DoorType.Rotating:
                StartCoroutine(RotateTo(initialRotation * Quaternion.Euler(rotateAngle)));
                break;
        }
    }
    
    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        
        if (obstacle != null) obstacle.enabled = true;

        StopAllCoroutines();
        
        switch (type)
        {
            case DoorType.Disappearing:
                gameObject.SetActive(true);
                break;
                
            case DoorType.Sliding:
                gameObject.SetActive(true);
                StartCoroutine(MoveTo(initialPosition));
                break;
                
            case DoorType.Rotating:
                gameObject.SetActive(true);
                StartCoroutine(RotateTo(initialRotation));
                break;
        }
    }
    
    private IEnumerator MoveTo(Vector3 targetPos)
    {
        float elapsed = 0;
        Vector3 startPos = transform.localPosition;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            // Smooth step interpolation
            t = t * t * (3f - 2f * t);
            
            transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        transform.localPosition = targetPos;
    }
    
    private IEnumerator RotateTo(Quaternion targetRot)
    {
        float elapsed = 0;
        Quaternion startRot = transform.localRotation;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            t = t * t * (3f - 2f * t);
            
            transform.localRotation = Quaternion.Lerp(startRot, targetRot, t);
            yield return null;
        }
        transform.localRotation = targetRot;
    }

    private void OnMouseDown()
    {
        if (requiresTask && isTaskLocked && !isOpen)
        {
            if (BPlusTreeTaskManager.Instance != null)
            {
                // Trigger Insertion Task for unlocking
                BPlusTreeTaskManager.Instance.StartTask(this, BPlusTreeTaskType.Insertion);
            }
        }
    }

    public void OnTaskComplete(bool success)
    {
        if (success)
        {
            Debug.Log("Door Unlocked by Task!");
            isTaskLocked = false;
            Open();
        }
        else
        {
            Debug.Log("Door Task Failed.");
        }
    }
}