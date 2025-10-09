using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Enemy Settings")]
    public float speed = 3.0f;
    public float rotation_speed = 100.0f;
    public float detection_range = 10.0f;

    [Header("References")]
    public Transform position;
    public Animator animator;
    public List<Transform> patrol_points;

    public void Start()
    {
        
    }

    public void Update()
    {
        
    }
}
