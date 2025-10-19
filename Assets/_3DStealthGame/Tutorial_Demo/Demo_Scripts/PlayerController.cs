using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using StealthGame;

public class PlayerController : MonoBehaviour
{
    public InputAction MoveAction;
    public InputAction WalkAction;

    public bool isWalking;
    public bool isMoving;
    
    public float walkSpeed = 0.5f;
    public float runSpeed = 1.0f;
    public float turnSpeed = 20f;

    private Animator m_Animator;
    private Rigidbody m_Rigidbody;
    private AudioSource m_AudioSource;
    private Vector3 m_Movement;
    private Quaternion m_Rotation = Quaternion.identity;
    // DEMO
    private List<string> m_OwnedKeys = new();

    void Start()
    {
        m_Animator = GetComponent<Animator>();
        m_Rigidbody = GetComponent<Rigidbody>();
        m_AudioSource = GetComponent<AudioSource>();

        MoveAction.Enable();
        WalkAction.Enable();
    }

    void FixedUpdate()
    {
        var pos = MoveAction.ReadValue<Vector2>();
    
        float horizontal = pos.x;
        float vertical = pos.y;
    
        m_Movement.Set(horizontal, 0f, vertical);
        m_Movement.Normalize();

        bool hasHorizontalInput = !Mathf.Approximately(horizontal, 0f);
        bool hasVerticalInput = !Mathf.Approximately(vertical, 0f);
        isMoving = hasHorizontalInput || hasVerticalInput;
        m_Animator.SetBool("IsWalking", isMoving);

        if (isMoving)
        {
            if (WalkAction.IsPressed())
            {
                m_AudioSource.volume = 0.5f;
                isWalking = true;
            }
            else
            {
                m_AudioSource.volume = 1f;
                isWalking = false;
            }
            
            if (!m_AudioSource.isPlaying)
                m_AudioSource.Play();
        }
        else
        {
            m_AudioSource.Stop();
        }

        float currentSpeed = WalkAction.IsPressed() ? walkSpeed : runSpeed;

        Vector3 desiredForward = Vector3.RotateTowards(transform.forward, m_Movement, turnSpeed * Time.deltaTime, 0f);
        m_Rotation = Quaternion.LookRotation(desiredForward);

        m_Rigidbody.MoveRotation(m_Rotation);
        m_Rigidbody.MovePosition(m_Rigidbody.position + m_Movement * currentSpeed * Time.deltaTime);
    }

    public void AddKey(string keyName)
    {
        m_OwnedKeys.Add(keyName);
    }

    public bool OwnKey(string keyName)
    {
        return m_OwnedKeys.Contains(keyName);
    }
}
