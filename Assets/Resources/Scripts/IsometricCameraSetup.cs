using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsometricCameraSetup : MonoBehaviour
{
    [Header("Isometric Camera Settings")]
    public Transform target; // Character or point to follow
    public float distance = 15f;
    public float height = 15f;
    public float orthographicSize = 12f;
    
    [Header("Isometric Angles")]
    public float xRotation = 30f; // Standard isometric: 30°
    public float yRotation = 45f; // Standard isometric: 45°
    
    [Header("Follow Settings")]
    public bool followTarget = true;
    public float followSpeed = 5f;
    
    private Camera cam;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        SetupIsometricCamera();
    }
    
    void Update()
    {
        if (followTarget && target != null)
        {
            FollowTarget();
        }
    }
    
    [ContextMenu("Setup Isometric Camera")]
    public void SetupIsometricCamera()
    {
        cam = GetComponent<Camera>();
        
        // Set camera to orthographic
        cam.orthographic = true;
        cam.orthographicSize = orthographicSize;
        
        // Set isometric rotation
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        
        // Position camera
        if (target != null)
        {
            Vector3 offset = new Vector3(-distance, height, -distance);
            transform.position = target.position + offset;
        }
        
        Debug.Log("Isometric camera setup complete!");
    }
    
    void FollowTarget()
    {
        Vector3 offset = new Vector3(-distance, height, -distance);
        Vector3 targetPosition = target.position + offset;
        
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
    }
    
    // Common isometric presets
    [ContextMenu("True Isometric (35.264°, 45°)")]
    public void SetTrueIsometric()
    {
        xRotation = 35.264f;
        yRotation = 45f;
        SetupIsometricCamera();
    }
    
    [ContextMenu("Classic Game Isometric (30°, 45°)")]
    public void SetClassicIsometric()
    {
        xRotation = 30f;
        yRotation = 45f;
        SetupIsometricCamera();
    }
    
    [ContextMenu("2:1 Isometric (26.565°, 45°)")]
    public void Set2to1Isometric()
    {
        xRotation = 26.565f;
        yRotation = 45f;
        SetupIsometricCamera();
    }
}