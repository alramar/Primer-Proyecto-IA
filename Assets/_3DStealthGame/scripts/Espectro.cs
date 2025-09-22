using System.Collections.Generic;
using UnityEngine;

// Espectro inherits from Enemy
public class Espectro : Enemy
{
    [Header("Espectro Specific Settings")]
    public List<Object> objects_to_possess;
    public float possession_range = 5.0f;
    public new void Start()
    {

    }

    // Update is called once per frame
    public new void Update()
    {

    }

    private void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    private void SetRotationSpeed(float newRotationSpeed)
    {
        rotation_speed = newRotationSpeed;
    }
    
    private void SetDetectionRange(float newDetectionRange)
    {
        detection_range = newDetectionRange;
    }
}
