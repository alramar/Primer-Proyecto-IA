using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public InputAction MoveAction;
    Animator m_Animator;

    public float walkSpeed = 1.0f;
    public float runSpeed = 2.0f;
    public float turnSpeed = 20f;
    public float currentSpeed;

    public bool isRunning;
    public bool isWalking;



    Rigidbody m_Rigidbody;
    Vector3 m_Movement;
    Quaternion m_Rotation = Quaternion.identity;

    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        MoveAction.Enable();
        m_Animator = GetComponent<Animator>();

        isRunning = false;
    }

    void FixedUpdate()
    {
        var pos = MoveAction.ReadValue<Vector2>();

        float horizontal = pos.x;
        float vertical = pos.y;
        
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) { currentSpeed = runSpeed; isRunning = true; }
        else { currentSpeed = walkSpeed; isRunning = false; }

        m_Movement.Set(horizontal, 0f, vertical);
        m_Movement.Normalize();

        Vector3 desiredForward = Vector3.RotateTowards(transform.forward, m_Movement, turnSpeed * Time.deltaTime, 0f);
        m_Rotation = Quaternion.LookRotation(desiredForward);

        isWalking = !Mathf.Approximately(horizontal,0f) || !Mathf.Approximately(vertical,0f);
        m_Animator.SetBool("isWalking", isWalking);

        m_Rigidbody.MoveRotation(m_Rotation);
        m_Rigidbody.MovePosition(m_Rigidbody.position + m_Movement * currentSpeed * Time.deltaTime);
    }
}
