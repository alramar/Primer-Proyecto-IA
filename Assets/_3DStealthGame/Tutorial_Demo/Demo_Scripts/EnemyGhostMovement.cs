using System;
using System.Collections.Generic;
using Assets.Scripts.Algorithms;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class EnemyGhostMovement : Enemy
{
    public GameObject obj;
    [SerializeField] public GameObject player;
    [SerializeField] public GameObject vision;
    [SerializeField] public GameObject beginPoint;
    [SerializeField] public Rigidbody rb;


    [SerializeField] private float posX1;
    [SerializeField] private float posZ1;
    [SerializeField] private float posX2;
    [SerializeField] private float posZ2;

    private float walkSpeed;
    [SerializeField] private float runSpeed;
    private float playerWalkRange;
    [SerializeField] private float playerRunRange;
    [SerializeField] private float vRange;
    Vector3 steer;
    public float maxForce;

    private float rotateSpeed;

    private int behaviour;
    private int patrollState;
    private float timeNoSee;

    Vector3 targetPoint;
    Vector3 normalPoint;
    Vector3 future;
    [SerializeField] private int currentPathIndex = 0;
    [SerializeField] float pathRadius;

    Graph graph;
    List<Node> path;
    Node currentPathObjective;
    Node currentNode;
    Node nextPathObjective;

    bool objectiveReached;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        obj = this.gameObject;
        walkSpeed = obj.GetComponent<Enemy>().speed;
        playerWalkRange = obj.GetComponent<Enemy>().detection_range;
        rotateSpeed = obj.GetComponent<Enemy>().rotation_speed;
        patrollState = 0;
        timeNoSee = 0f;
        behaviour = 1; //0 = idle, 1 = patrulla, 2 = perseguir
    }

    // Update is called once per frame
    void Update()
    {
        /*if (player.GetComponent<PlayerMovement>().isRunning || player.GetComponent<PlayerMovement>().isWalking)
        {
            float distance = Vector3.Distance(obj.transform.position, player.transform.position);

            if (player.GetComponent<PlayerMovement>().isRunning && distance <= playerRunRange) { behaviour = 2; }
            if (player.GetComponent<PlayerMovement>().isWalking && distance <= playerWalkRange) { behaviour = 2; }
        }*/

        /*switch (behaviour)
        {
            case 0: break;
            case 1:
                
                patroll();
                break;
            default: chase(); break;
        }*/
        if (EnemyGhostVisor.seePlayer) { behaviour = 2; }
        else
        {
            if (timeNoSee >= 3f) { behaviour = 3; timeNoSee = 0f; }
            else { timeNoSee += Time.deltaTime; }
        }

        if (behaviour == 1) 
        {
            if (transform.position == new Vector3(posX1, transform.position.y, posZ1))
            { patrollState = 0; transform.Rotate(new Vector3(0f, 0f, 180f)); }
            if (transform.position == new Vector3(posX2, transform.position.y, posZ2))
            { patrollState = 1; transform.Rotate(new Vector3(0f, 0f, 180f)); }
            patroll(); 
        }
        else if (behaviour == 2) { chase(); }
    }

    void patroll()
    {
        float step = walkSpeed * Time.deltaTime;
        if (patrollState == 0) { transform.position = Vector3.MoveTowards(transform.position, new Vector3(posX2, transform.position.y, posZ2), step); }
        else { transform.position = Vector3.MoveTowards(transform.position, new Vector3(posX1, transform.position.y, posZ1), step); }
    }

    void chase()
    {

        //transform.position = Vector3.MoveTowards(transform.position, player.transform.position, runSpeed * Time.deltaTime);
        currentPathObjective = graph.GetClosestNode(player.transform);
        currentNode = graph.GetClosestNode(transform);

        path = new();
        path = graph.TryPathing(currentNode, currentPathObjective);
        FollowStarPath();
        

    }

    void Return() 
    {
        if (transform.position == beginPoint.transform.position) { behaviour = 1; }
        else
        {
            currentPathObjective = graph.GetClosestNode(beginPoint.transform);
            currentNode = graph.GetClosestNode(transform);

            path = new();
            path = graph.TryPathing(currentNode, currentPathObjective);
            FollowStarPath();
        }
        
    }

    void FollowStarPath()
    {
        if (path == null || path.Count < 2) return;
        targetPoint = Vector3.zero;
        float lookAhead = Math.Clamp(rb.linearVelocity.magnitude, 0.5f, 1f);
        future = transform.position + rb.linearVelocity.normalized * lookAhead;
        Vector3 a = path[currentPathIndex].transform.position;
        Vector3 b = path[currentPathIndex + 1].transform.position;
        float t;
        normalPoint = GetNormalPoint(future, a, b, out t);

        if (t >= 1f)
        {
            if (currentPathIndex < path.Count - 2)
            {
                currentPathIndex++;
            }
            else
            {
                objectiveReached = true;
                currentPathIndex = 0;
            }
        }
        Vector3 direction = (b - a).normalized;
        targetPoint = normalPoint + direction * lookAhead;
        FollowPath();
    }

    void FollowPath()
    {
        float dist = Vector3.Distance(future, normalPoint);
        if (dist > pathRadius)
            Seek(targetPoint);
    }

    Vector3 GetNormalPoint(Vector3 future, Vector3 start, Vector3 finish, out float t)
    {
        // Vector3 iniSeccionAfuture = future - start;
        // Vector3 proyeccion = finish - start;
        // proyeccion = proyeccion.normalized * Vector3.Dot(iniSeccionAfuture, proyeccion);
        // return start + proyeccion;


        Vector3 ap = future - start;
        Vector3 ab = finish - start;

        t = Vector3.Dot(ap, ab) / ab.sqrMagnitude;
        return start + ab * t;


    }

    void Seek(Vector3 target)
    {
        float currentSpeed = speed;
        if (behaviour == 2) { currentSpeed = runSpeed; }
        Vector3 desired = target - transform.position;
        desired = desired.normalized * runSpeed;
        steer = desired - rb.linearVelocity;
        steer = Vector3.ClampMagnitude(steer, maxForce);
    }
}

