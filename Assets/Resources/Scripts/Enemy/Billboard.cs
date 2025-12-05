using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    public bool freezeXZAxis = true; 

    void LateUpdate()
    {
        if (Camera.main != null)
        {
            // Make the object face the same direction as the camera
            transform.rotation = Camera.main.transform.rotation;
            
            if (freezeXZAxis)
            {
                // Reset X and Z rotation so it stands upright
                Vector3 euler = transform.rotation.eulerAngles;
                transform.rotation = Quaternion.Euler(0, euler.y, 0);
            }
        }
    }
}