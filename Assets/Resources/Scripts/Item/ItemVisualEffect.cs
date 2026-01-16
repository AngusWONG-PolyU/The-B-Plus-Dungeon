using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemVisualEffect : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 45f;

    [Header("Floating Settings")]
    public float floatAmplitude = 0.25f; // Height of the float
    public float floatFrequency = 1f; // Speed of the float

    private Vector3 startPos;
    private bool isInitialized = false;

    void Start()
    {
        // Capture the initial position as the baseline for floating
        if (!isInitialized)
        {
            startPos = transform.position;
            isInitialized = true;
        }
    }

    void OnEnable()
    {
        // Backup in case Start hasn't run yet or to reset
        if (!isInitialized)
        {
            startPos = transform.position;
            isInitialized = true;
        }
    }

    void Update()
    {
        // 1. Rotation (Rotate around Y axis)
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // 2. Floating (Sin wave on Y axis)
        float newY = startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
