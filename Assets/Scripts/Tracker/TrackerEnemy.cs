using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Algorithms;
using StealthGame;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(GameObject))]
public class TrackerEnemy : Enemy
{
    public EnemyStateMachine stateMachine;
    Vector3 steer;
    Vector3 acceleration;
    public Rigidbody rb;
    public PlayerMovement target;
    [SerializeField] private PlayerMovement playerRef;

    [Header("Wander Options")]
    [SerializeField]
    float wanderAngle = 0;
    public float maxForce;
    [SerializeField]
    float wanderRadius = 1;
    Vector3 wanderPoint;
    Vector3 wanderCirclePosition;
    float nextWanderTimer = 0;
    [SerializeField]
    float nextWanderTemp;

    [Header("Pursuit settings")]
    public float pursuitTimer = 0f;
    [SerializeField]
    public float pursuitTime;

    [Header("Path Settings")]
    List<Node> path;
    List<Node> visitedNodes;
    Node lastPathNodeVisited;
    Node currentPathObjective;
    Node nextPathObjective;
    public bool objectiveReached = true;
    bool isFar = true;
    [SerializeField]
    float farAwayPatrolRadius = 0;
    [SerializeField]
    float nearbyPatrolRadius = 0;
    [SerializeField] private int currentPathIndex = 0;
    [SerializeField] private bool forward = true;
    [SerializeField] float pathMagnetism;
    [SerializeField]
    Graph graph;
    [SerializeField]
    public float pathRadius;
    Vector3 targetPoint;
    Vector3 normalPoint;
    Vector3 future;
    [Header("Vision Settings")]
    [SerializeField]
    float rayDistance;
    [SerializeField]
    float degreeRotationRange;
    [SerializeField]
    LayerMask layerMask;
    NativeArray<RaycastHit> results;
    NativeArray<RaycastCommand> commands;
    QueryParameters queryParameters;
    JobHandle raycastJobHandle;
    bool jobScheduled = false;
    [SerializeField]
    Transform eyeHeight;
    [SerializeField]
    public PlayerTracks playerTracks;
    public bool isFollowingTracks = false;
    List<GameObject> trackPath;
    GameObject currentTrackPathObject;
    int currentTrackPathIndex = 0;
    [HideInInspector] public Vector3 lastKnownTargetPosition;



    public Queue<GameObject> trackQueue = new Queue<GameObject>();
    private GameObject currentTrackTarget;
    public float rotationSpeed = 5f;



    void Awake()
    {
        stateMachine = new();
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        visitedNodes = new();

    }

    new void Start()
    {
        stateMachine.Initialize(new PatrolState(), this);
        SetUpVisionRayCasts();
        if (playerRef == null)
        {
            var go = GameObject.FindWithTag("Player");
            if (go != null) playerRef = go.GetComponent<PlayerMovement>();
            if (playerRef == null)
                Debug.LogWarning("[TrackerEnemy] No se ha asignado playerRef y no se encontró un GameObject con Tag 'Player' con PlayerMovement.");
        }

    }

    void FixedUpdate()
    {
        PrepareRayCasts();
        stateMachine.Update(this);
        Move();
        // string x = "NAH";
        // if (target != null) x = target.name; 
        // Debug.Log("ayudaa " + x);
        // PrepareRayCasts();
        // // if (target == null && isFollowingTracks)
        // // {
        // //     Debug.Log("FOLLOWING TRACKSSS");
        // //     FollowPlayerTracks();
        // // }
        // // else 
        // if (target != null)
        // {
        //     Debug.Log("FOLLOWING YOUUUU");
        //     Pursuit();
        //     pursuitTimer += Time.fixedDeltaTime;
        //     if (pursuitTimer >= pursuitTime)
        //     {
        //         target = null;
        //         pursuitTimer = 0;
        //     }
        //     Move();
        // }
        // // else
        // // {
        // //     Debug.Log("Patrochilling");
        // //     Patrol();
        // // }

    }

    void LateUpdate()
    {
        HandleVision();
    }

    public void FollowPlayerTracks()
    {
        // Si no hay rastros, terminamos
        if (playerTracks == null || playerTracks.playerTracksGONodes.Count == 0)
        {
            isFollowingTracks = false;
            return;
        }

        // Si la cola está vacía o desactualizada, la refrescamos
        if (trackQueue.Count == 0 || trackQueue.LastOrDefault() != playerTracks.playerTracksGONodes.LastOrDefault())
        {
            trackQueue = new Queue<GameObject>(playerTracks.playerTracksGONodes);
        }

        // Si no hay target actual, tomar el primero
        if (currentTrackTarget == null && trackQueue.Count > 0)
        {
            currentTrackTarget = trackQueue.Peek();
        }

        if (currentTrackTarget == null) return;

        Vector3 targetPos = currentTrackTarget.transform.position;
        Seek(targetPos); // usamos tu versión original de Seek()

        float dist = Vector3.Distance(transform.position, targetPos);
        if (dist <= 1.2f)
        {
            // Pasar al siguiente nodo
            trackQueue.Dequeue();
            currentTrackTarget = trackQueue.Count > 0 ? trackQueue.Peek() : null;
        }

        // Si se acaban los nodos
        if (trackQueue.Count == 0)
            isFollowingTracks = false;
    }

    public void Pursuit()
    {
        Vector3 pursuitTargetPos;

        if (target != null)
        {
            // Se ve al jugador
            lastKnownTargetPosition = target.transform.position;
            pursuitTargetPos = lastKnownTargetPosition;
        }
        else
        {
            // No se ve, ir hacia la última posición conocida
            pursuitTargetPos = lastKnownTargetPosition;
        }

        //TODO
        //TODO
        //TODOOOO hacer que algo similar a patrol para seguir el path hasta el nodo más cercano al jugador en todo momento.
        List<Node> ap = graph.TryPathing(transform, target.transform);

        if (ap == null || ap.Count < 2) return;
        targetPoint = Vector3.zero;
        float lookAhead = Math.Clamp(rb.linearVelocity.magnitude, 0.5f, 1f);
        future = transform.position + rb.linearVelocity.normalized * lookAhead;
        Vector3 a = ap[currentPathIndex].transform.position;
        Vector3 b = ap[currentPathIndex + 1].transform.position;
        float t;
        normalPoint = GetNormalPoint(future, a, b, out t);

        if (t >= 1f)
        {
            if (currentPathIndex < ap.Count - 2)
            {
                currentPathIndex++;
            }
            else
            {
                objectiveReached = true;
                isFar = !isFar;
                currentPathIndex = 0;
            }
        }
        Vector3 direction = (b - a).normalized;
        targetPoint = normalPoint + direction * lookAhead;
        FollowPath();

        Seek(pursuitTargetPos); // usa el Seek ya existente
    }

    void HandleVision()
    {
        if (!jobScheduled) return;

        // Aseguramos que los rayos hayan terminado de procesarse
        raycastJobHandle.Complete();
        jobScheduled = false;

        bool playerSeen = false;
        bool trackSeen = false;

        foreach (RaycastHit hit in results)
        {
            if (hit.collider == null) continue;

            // --- Detectar al jugador ---
            if (hit.collider.CompareTag("Player"))
            {
                if (playerRef != null)
                {
                    playerSeen = true;
                    target = playerRef;
                    pursuitTimer = 0f;
                    lastKnownTargetPosition = playerRef.transform.position;
                }
                else
                {
                    // fallback: intentar GetComponentInParent o attachedRigidbody
                    PlayerMovement pm = hit.collider.GetComponentInParent<PlayerMovement>()
                                      ?? hit.collider.attachedRigidbody?.GetComponent<PlayerMovement>();
                    if (pm != null)
                    {
                        playerSeen = true;
                        target = pm;
                        pursuitTimer = 0f;
                        lastKnownTargetPosition = pm.transform.position;
                    }
                    else
                    {
                        Debug.LogWarning("[TrackerEnemy] Collider golpeado tiene Tag 'Player' pero no se pudo obtener PlayerMovement desde el collider.");
                    }
                }
            }

            // --- Detectar rastros del jugador ---
            else if (hit.collider.GetComponentInParent<PlayerNode>() != null)
            {
                trackSeen = true;
            }
        }

        if (playerSeen)
        {
            if (stateMachine.currentState is not PursuitState)
                stateMachine.ChangeState(new PursuitState(), this);
        }
        else if (trackSeen)
        {
            if (stateMachine.currentState is not FollowTracksState)
                stateMachine.ChangeState(new FollowTracksState(), this);
        }
    }


    public void Patrol()
    {
        if (objectiveReached)
        {
            path = new();
            currentPathIndex = 0;
            while (path.Count == 0)
            {
                if (isFar)
                {
                    nextPathObjective = graph.GetFurthestNodeInRadius(lastPathNodeVisited != null ? lastPathNodeVisited.transform : transform, farAwayPatrolRadius, visitedNodes);
                }
                else
                {
                    nextPathObjective = graph.GetFurthestNodeInRadius(lastPathNodeVisited != null ? lastPathNodeVisited.transform : transform, nearbyPatrolRadius, visitedNodes);
                }
                if (currentPathObjective)
                {
                    visitedNodes.Add(currentPathObjective);
                    lastPathNodeVisited = currentPathObjective;

                }
                currentPathObjective = nextPathObjective;
                objectiveReached = false;
                path = graph.TryPathing(transform, currentPathObjective);
                //Debug.Log(path.Count);

            }
        }
        else
        {
            FollowAStarPath();
        }


    }


    public void Seek(Vector3 target)
    {
        Vector3 desired = target - transform.position;
        desired = desired.normalized * speed;
        steer = desired - rb.linearVelocity;
        steer = Vector3.ClampMagnitude(steer, maxForce);
    }


    public void Pursuit_OLD()
    {
        if (target == null) return;
        Vector3 prediction = target.transform.position + target.velocity * 10;
        Seek(prediction);
    }

    void OnDrawGizmos()
    {

        if (path == null || path.Count < 2) return;

        // 1) Dibujar el tubo del path con esferas interpoladas
        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3 a = path[i].transform.position;
            Vector3 b = path[i + 1].transform.position;
            Gizmos.color = new Color(0, 1, 0, 0.1f); // verde con transparencia

            int steps = 12; // más pasos = tubo más suave
            for (int j = 0; j <= steps; j++)
            {
                float t = j / (float)steps;
                Vector3 p = Vector3.Lerp(a, b, t);
                Gizmos.DrawWireSphere(p, pathRadius);
            }
        }

        // 2) Dibujar radio del enemigo, verde si está dentro, rojo si está fuera
        if (Application.isPlaying)
        {
            float dist = Vector3.Distance(future, normalPoint);
            Gizmos.color = (dist <= pathRadius) ? Color.green : Color.red;
            Gizmos.DrawWireSphere(future, pathRadius);
        }

        // Gizmos.DrawWireSphere(wanderCirclePosition, wanderRadius);
        // Gizmos.color = Color.green;
        // Gizmos.DrawLine(wanderCirclePosition, wanderPoint);
        // Gizmos.color = Color.blue;
        // Gizmos.DrawLine(transform.position, wanderCirclePosition);
        Gizmos.color = Color.white;
        for (int i = 0; i < path.Count - 1; i++)
        {
            Gizmos.DrawLine(path[i].transform.position, path[i + 1].transform.position);
        }
        //Gizmos.DrawLineList(path);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, future);
        Gizmos.DrawWireSphere(future, 0.2f);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, targetPoint);
        Gizmos.DrawWireSphere(targetPoint, 0.2f);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(normalPoint, 0.2f);
        Gizmos.color = Color.gray;
        foreach (var command in commands)
        {
            Gizmos.DrawRay(eyeHeight.position, command.direction * rayDistance);

        }
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(currentPathObjective.transform.position, Vector3.one * 0.2f);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(nextPathObjective.transform.position, Vector3.one * 0.2f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(nextPathObjective.transform.position, Vector3.one * 0.2f);

#if UNITY_EDITOR
        // Etiqueta con la distance real al path
        if (Application.isPlaying)
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f,
                $"Distance={Vector3.Distance(future, normalPoint):F2}");
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1f,
        $"CurrentPoint={currentPathIndex:F2}");
#endif

    }


    public void Wander()
    {
        wanderCirclePosition = transform.position + 1 * rb.linearVelocity;
        wanderAngle += Random.value * 0.5f - 0.25f;
        wanderPoint = new Vector3(wanderCirclePosition.x + math.cos(wanderAngle) * wanderRadius, wanderCirclePosition.y, wanderCirclePosition.z + math.sin(wanderAngle) * wanderRadius);
        Seek(wanderPoint);
    }

    public void Move()
    {
        if (Vector3.Angle(transform.forward, rb.linearVelocity) > 0.1f)
        {
            transform.forward = Vector3.RotateTowards(transform.forward, rb.linearVelocity, rotation_speed, maxForce);
        }

        rb.AddForce(steer, ForceMode.Acceleration);
    }


    public void SetUpVisionRayCasts()
    {
        results = new NativeArray<RaycastHit>(5, Allocator.Persistent);
        commands = new NativeArray<RaycastCommand>(5, Allocator.Persistent);
        queryParameters = new QueryParameters(layerMask, false, QueryTriggerInteraction.Collide);
    }

    public void PrepareRayCasts()
    {
        for (int i = 1; i <= commands.Length; i++)
        {
            Vector3 direction = Quaternion.AngleAxis(degreeRotationRange / commands.Length * i - degreeRotationRange * 0.5f, Vector3.up) * transform.forward;
            direction = Quaternion.AngleAxis(5, Vector3.right) * direction;

            commands[i - 1] = new RaycastCommand(eyeHeight.position, direction, queryParameters, rayDistance);

        }
        raycastJobHandle = RaycastCommand.ScheduleBatch(commands, results, 1, 1);
        jobScheduled = true;
    }

    public void CalculateFollowPath()
    {
        if (path == null || path.Count < 2) return;
        targetPoint = Vector3.zero;
        normalPoint = Vector3.zero;
        Vector3 bestDirection = Vector3.zero;
        float lookAhead = Mathf.Clamp(rb.linearVelocity.magnitude * 0.5f, 1f, 5f);
        future = transform.position + rb.linearVelocity.normalized * lookAhead;
        // Escogemos segmento actual según dirección
        int nextIndex = forward ? currentPathIndex + 1 : currentPathIndex - 1;
        nextIndex = Mathf.Clamp(nextIndex, 0, path.Count - 1);

        Vector3 a = path[currentPathIndex].transform.position;
        Vector3 b = path[nextIndex].transform.position;
        float t;
        Vector3 possibleNormalPoint = GetNormalPoint(future, a, b, out t);
        if (t >= 1f)
        {
            if (forward)
            {
                if (currentPathIndex < path.Count - 2)
                {
                    currentPathIndex++;
                }
                else forward = false;
            }
            else
            {

                if (currentPathIndex > 1)
                {
                    currentPathIndex--;
                }
                else forward = true;

            }
        }
        // Direccion del segmento
        bestDirection = (b - a).normalized;

        normalPoint = possibleNormalPoint;
        targetPoint = normalPoint + bestDirection * lookAhead;
        FollowPath();
    }

    public void FollowAStarPath()
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
                isFar = !isFar;
                currentPathIndex = 0;
            }
        }
        Vector3 direction = (b - a).normalized;
        targetPoint = normalPoint + direction * lookAhead;
        FollowPath();

    }

    public void FollowPath()
    {
        // Solo seguir si estamos fuera del radio
        float dist = Vector3.Distance(future, normalPoint);
        if (dist > pathRadius)
            Seek(targetPoint);

    }

    public void FollowPlayerTracks_OLD()
    {
        trackPath = playerTracks.playerTracksGONodes.ToList();
        currentPathIndex = trackPath.IndexOf(currentTrackPathObject);
        if (trackPath == null || trackPath.Count < 2 || trackPath[currentTrackPathIndex + 1] == null)
        {
            isFollowingTracks = false;
            return;
        }
        ;
        targetPoint = Vector3.zero;
        float lookAhead = Math.Clamp(rb.linearVelocity.magnitude, 0.5f, 1f);
        future = transform.position + rb.linearVelocity.normalized * lookAhead;
        Vector3 a = trackPath[currentTrackPathIndex].transform.position;
        Vector3 b = trackPath[currentTrackPathIndex + 1].transform.position;
        float t;
        normalPoint = GetNormalPoint(future, a, b, out t);

        if (t >= 1f)
        {
            if (currentTrackPathIndex < trackPath.Count - 2)
            {
                currentTrackPathIndex++;
                currentTrackPathObject = trackPath[currentTrackPathIndex];
            }
            else
            {
                currentPathIndex = 0;
                currentTrackPathObject = null;
                trackPath = null;
                isFollowingTracks = false;
                target = playerTracks.player.GetComponent<PlayerMovement>();
            }
        }
        Vector3 direction = (b - a).normalized;
        targetPoint = normalPoint + direction * lookAhead;
        FollowPath();

    }


    public Vector3 GetNormalPoint(Vector3 future, Vector3 start, Vector3 finish, out float t)
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
    void OnDestroy()
    {
        // Liberar memoria persistente
        if (commands.IsCreated) commands.Dispose();
        if (results.IsCreated) results.Dispose();
    }

}
