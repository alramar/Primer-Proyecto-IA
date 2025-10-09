using System.Collections.Generic;
using Assets.Scripts.Algorithms;
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
    Vector3 steer;
    Vector3 acceleration;
    Rigidbody rb;
    PlayerMovement target;
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
    float pursuitTimer = 0;
    [SerializeField]
    float pursuitTime;

    [Header("Path Settings")]
    List<Node> path;
    List<Node> visitedNodes;
    Node lastPathNodeVisited;
    Node currentPathObjective;
    Node nextPathObjective;
    bool objectiveReached = true;
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
    float pathRadius;
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

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        SetUpVisionRayCasts();
    }


    void FixedUpdate()
    {
        Patrol();
        Move();
    }

    void LateUpdate()
    {
        if (!jobScheduled) return;

        // Asegurarse de que el job terminó antes de acceder a resultados
        raycastJobHandle.Complete();

        // Procesar resultados
        foreach (RaycastHit hit in results)
        {
            if (hit.collider != null)
            {
                if (hit.collider.CompareTag("Player"))
                {
                    hit.collider.TryGetComponent(out target);
                    pursuitTimer = 0;
                }
                else
                {
                    Seek(transform.position + hit.normal * 3);
                }
            }

        }

        jobScheduled = false; // listo para el próximo frame
    }

    void Patrol()
    {
        if (objectiveReached)
        {
            if (currentPathObjective)
            {
                visitedNodes.Add(currentPathObjective);
                lastPathNodeVisited = currentPathObjective;
                currentPathObjective = nextPathObjective;
                nextPathObjective = graph.GetFirstNodeInRadius(lastPathNodeVisited.transform, nearbyPatrolRadius);
            }
            else
            {
                currentPathObjective = graph.GetFirstNodeInRadius(transform, farAwayPatrolRadius);
                nextPathObjective = graph.GetFirstNodeInRadius(currentPathObjective.transform, farAwayPatrolRadius);
            }
            path = graph.TryPathing(transform, currentPathObjective);
            objectiveReached = false;
        }
        else
        {
            CalculateFollowPath();
            FollowPath();
        }
        
        
    }


    void Seek(Vector3 target)
    {
        Vector3 desired = target - transform.position;
        desired = desired.normalized * speed;
        steer = desired - rb.linearVelocity;
        steer = Vector3.ClampMagnitude(steer, maxForce);
    }


    void Pursuit()
    {
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

#if UNITY_EDITOR
        // Etiqueta con la distance real al path
        if (Application.isPlaying)
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f,
                $"Distance={Vector3.Distance(future, normalPoint):F2}");
                UnityEditor.Handles.Label(transform.position + Vector3.up * 1f,
                $"CurrentPoint={currentPathIndex:F2}");
#endif

    }


    void Wander()
    {
        wanderCirclePosition = transform.position + 1 * rb.linearVelocity;
        wanderAngle += Random.value * 0.5f - 0.25f;
        wanderPoint = new Vector3(wanderCirclePosition.x + math.cos(wanderAngle) * wanderRadius, wanderCirclePosition.y, wanderCirclePosition.z + math.sin(wanderAngle) * wanderRadius);
        Seek(wanderPoint);
    }

    void Move()
    {
        transform.forward = Vector3.RotateTowards(transform.forward, rb.linearVelocity, rotation_speed, maxForce);
        rb.AddForce(steer, ForceMode.Acceleration);
    }

    // void FollowPath()
    // {
    //     targetPoint = Vector3.zero;
    //     float bestDistance = float.PositiveInfinity;
    //     Vector3 future = transform.position;
    //     int n = path.Length - 1;
    //     //for (int i = n; i < 2 * n; i++)
    //     for (int i = 0; i < n; i++)
    //     {
    //         // int current_index = math.abs(i - n);
    //         // int next_index = math.abs((i + 1) % (2 * n) - n);
    //         int current_index = 1;
    //         int next_index = i + 1;
    //         Vector3 a = path[current_index];
    //         Vector3 b = path[next_index];
    //         Vector3 possibleNormalPoint = GetNormalPoint(future, a, b);
    //         Vector3 direccion = b - a;
    //         if (possibleNormalPoint.x < math.min(a.x, b.x) ||
    //             possibleNormalPoint.x > math.max(a.x, b.x) ||
    //             possibleNormalPoint.y < math.min(a.y, b.y) ||
    //             possibleNormalPoint.y > math.max(a.y, b.y))
    //         {
    //             possibleNormalPoint = b;
    //             // If we're at the end we really want the next line segment for looking ahead
    //             a = path[next_index];
    //             b = path[next_index]; // Path wraps around
    //             direccion = b - a;
    //         }
    //         float distance = Vector3.Distance(possibleNormalPoint, future);
    //         //Debug.Log(distance);
    //         if (distance < bestDistance)
    //         {
    //             bestDistance = distance;
    //             normalPoint = possibleNormalPoint;
    //             direccion = (b - a).normalized;

    //             // Aquí decides cuánto "mirar hacia adelante"
    //             float lookAhead = 1.5f; // ajusta según velocidad

    //             // El target real estará más adelante
    //             targetPoint = normalPoint + direccion * lookAhead;
    //         }
    //     }
    //     //Debug.Log("Best: " + bestDistance);
    //     if (bestDistance > pathRadius && targetPoint != Vector3.zero)
    //     {
    //         Debug.Log("target: " + targetPoint);
    //         Seek(targetPoint);
    //     }
    //     //else if (rb.linearVelocity.sqrMagnitude <= 0.1) rb.linearVelocity = transform.forward * speed;
    // }
    void SetUpVisionRayCasts()
    {
        results = new NativeArray<RaycastHit>(5, Allocator.Persistent);
        commands = new NativeArray<RaycastCommand>(5, Allocator.Persistent);
        queryParameters = new QueryParameters(layerMask, false, QueryTriggerInteraction.Collide);
    }

    void PrepareRayCasts()
    {
        for (int i = 1; i <= commands.Length; i++)
        {
            Vector3 direction = Quaternion.AngleAxis(degreeRotationRange / commands.Length * i - degreeRotationRange * 0.5f, Vector3.up) * transform.forward;

            commands[i - 1] = new RaycastCommand(eyeHeight.position, direction, queryParameters, rayDistance);

        }
    }

    void CalculateFollowPath()
    {
        if (path == null || path.Count < 2) return;
        targetPoint = Vector3.zero;
        float bestDistance = float.PositiveInfinity;
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
    }
    void FollowPath()
    {
        // Solo seguir si estamos fuera del radio
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
    void OnDestroy()
    {
        // Liberar memoria persistente
        if (commands.IsCreated) commands.Dispose();
        if (results.IsCreated) results.Dispose();
    }

}
