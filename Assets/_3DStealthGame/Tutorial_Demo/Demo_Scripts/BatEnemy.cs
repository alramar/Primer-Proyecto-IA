using System.Collections.Generic;
using UnityEngine;

public class BatEnemy : MonoBehaviour
{
    private GameEnding gameEnding;
    private GameObject player;
    private PlayerController playerController;
    private Collider playerCollider;

    public enum Estado { Patrulla, Persecucion }
    private Estado estadoActual = Estado.Patrulla;

    [Header("Detección")]
    public float runDetectionRadius = 1.5f;
    public float walkDetectionRadius = 0.6f;
    private SphereCollider soundCollider;

    [Header("Movimiento")]
    public float speed = 2f;
    public Transform[] waypoints;
    private int currentWaypointIndex = 0;

    private Vector3 ultimaPosicionConocida;
    private bool playerDetectado = false;

    private NodeGraphGenerator nodeGraph;
    private List<Node> path = new List<Node>();
    private int currentPathIndex = 0;
    private Vector3 lastTargetPosition;

    void Start()
    {
        gameEnding = GameObject.FindFirstObjectByType<GameEnding>();
        player = GameObject.FindGameObjectWithTag("Player");
        playerController = player.GetComponent<PlayerController>();
        playerCollider = player.GetComponent<Collider>();

        soundCollider = GameObject.FindGameObjectWithTag("SoundCollider").GetComponent<SphereCollider>();
        soundCollider.radius = runDetectionRadius;
        soundCollider.enabled = true;

        nodeGraph = GameObject.FindFirstObjectByType<NodeGraphGenerator>();
    }

    void Update()
    {
        soundCollider.radius = playerController.isWalking ? walkDetectionRadius : runDetectionRadius;

        if (estadoActual == Estado.Patrulla)
        {
            Patrullar();
        }
        else if (estadoActual == Estado.Persecucion)
        {
            Perseguir();
        }
    }

    void Patrullar()
    {
        if (playerDetectado)
        {
            CambiarEstado(Estado.Persecucion);
            return;
        }
        if (waypoints.Length == 0) return;

        Vector3 targetPos = waypoints[currentWaypointIndex].position;
        MoverseHacia(targetPos);

        if (Vector3.Distance(transform.position, targetPos) < 0.2f)
        {
            // Loop infinito de waypoints
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
    }

    void Perseguir()
    {
        if (playerDetectado)
        {
            ultimaPosicionConocida = player.transform.position;
        }

        MoverseHacia(ultimaPosicionConocida);

        if (RutaCompletada())
        {
            // Buscar waypoint más cercano al volver a patrullar
            currentWaypointIndex = FindClosestWaypointIndex();
            CambiarEstado(Estado.Patrulla);
        }
    }

    void MoverseHacia(Vector3 destino)
    {
        if (nodeGraph == null || nodeGraph.nodes.Count == 0) return;

        Node startNode = FindClosestNode(transform.position);
        Node targetNode = FindClosestNode(destino);
        if (startNode == null || targetNode == null) return;

        if (path.Count == 0 || Vector3.Distance(destino, lastTargetPosition) > 0.5f)
        {
            path = AStar(startNode, targetNode);
            currentPathIndex = 0;
            lastTargetPosition = destino;
        }
        if (path.Count == 0) return;

        Vector3 nextPos = path[currentPathIndex].position;
        Vector3 direction = (nextPos - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
        }

        if (Vector3.Distance(transform.position, nextPos) < 0.1f && currentPathIndex < path.Count - 1)
        {
            currentPathIndex++;
        }
    }

    int FindClosestWaypointIndex()
    {
        int closestIndex = 0;
        float minDist = float.MaxValue;
        for (int i = 0; i < waypoints.Length; i++)
        {
            float dist = Vector3.Distance(transform.position, waypoints[i].position);
            if (dist < minDist)
            {
                minDist = dist;
                closestIndex = i;
            }
        }
        return closestIndex;
    }

    Node FindClosestNode(Vector3 position)
    {
        Node closest = null;
        float minDist = float.MaxValue;
        foreach (Node n in nodeGraph.nodes)
        {
            float dist = Vector3.Distance(position, n.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = n;
            }
        }
        return closest;
    }

    List<Node> AStar(Node start, Node goal)
    {
        var openSet = new PriorityQueue<Node>();
        var cameFrom = new Dictionary<Node, Node>();
        var gScore = new Dictionary<Node, float>();
        var fScore = new Dictionary<Node, float>();
        var closedSet = new HashSet<Node>();

        foreach (var node in nodeGraph.nodes)
        {
            gScore[node] = float.MaxValue;
            fScore[node] = float.MaxValue;
        }
        gScore[start] = 0;
        fScore[start] = Vector3.Distance(start.position, goal.position);

        openSet.Enqueue(start, fScore[start]);

        while (openSet.Count > 0)
        {
            Node current = openSet.Dequeue();
            if (current == goal)
                return ReconstructPath(cameFrom, current);

            closedSet.Add(current);

            foreach (Node neighbor in current.neighbours)
            {
                if (closedSet.Contains(neighbor))
                    continue;

                float tentativeG = gScore[current] + Vector3.Distance(current.position, neighbor.position);
                if (tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Vector3.Distance(neighbor.position, goal.position);

                    if (!openSet.Contains(neighbor))
                        openSet.Enqueue(neighbor, fScore[neighbor]);
                }
            }
        }
        return new List<Node>();
    }

    List<Node> ReconstructPath(Dictionary<Node, Node> cameFrom, Node current)
    {
        var totalPath = new List<Node> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Insert(0, current);
        }
        return totalPath;
    }

    bool RutaCompletada()
    {
        if (path == null || path.Count == 0) return true;
        return Vector3.Distance(transform.position, path[path.Count - 1].position) < 0.1f;
    }

    void CambiarEstado(Estado nuevoEstado)
    {
        estadoActual = nuevoEstado;
        path.Clear();
        currentPathIndex = 0;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == playerCollider)
        {
            playerDetectado = true;
            ultimaPosicionConocida = player.transform.position;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other == playerCollider)
        {
            playerDetectado = false;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other == playerCollider)
            ultimaPosicionConocida = player.transform.position;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider == playerCollider)
        {
            if (gameEnding != null)
                gameEnding.CaughtPlayer();
        }
    }

    void OnDrawGizmos()
    {
        if (soundCollider != null)
        {
            Gizmos.color = Color.red;
            float worldRadius = soundCollider.radius * Mathf.Max(
                transform.lossyScale.x,
                transform.lossyScale.y,
                transform.lossyScale.z
            );
            Gizmos.DrawWireSphere(soundCollider.transform.position, worldRadius);
        }

        if (path != null && path.Count > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < path.Count - 1; i++)
                Gizmos.DrawLine(path[i].position, path[i + 1].position);
        }
    }

    private class PriorityQueue<T>
    {
        private List<(T item, float priority)> elements = new List<(T, float)>();
        public int Count => elements.Count;
        public bool Contains(T item) => elements.Exists(e => EqualityComparer<T>.Default.Equals(e.item, item));

        public void Enqueue(T item, float priority)
        {
            elements.Add((item, priority));
            int ci = elements.Count - 1;
            while (ci > 0)
            {
                int pi = (ci - 1) / 2;
                if (elements[ci].priority < elements[pi].priority)
                {
                    (elements[ci], elements[pi]) = (elements[pi], elements[ci]);
                    ci = pi;
                }
                else break;
            }
        }

        public T Dequeue()
        {
            int li = elements.Count - 1;
            (T item, float priority) front = elements[0];
            elements[0] = elements[li];
            elements.RemoveAt(li);
            --li;
            int pi = 0;
            while (true)
            {
                int left = 2 * pi + 1;
                int right = 2 * pi + 2;
                if (left > li) break;
                int minIndex = (right <= li && elements[right].priority < elements[left].priority) ? right : left;
                if (elements[pi].priority > elements[minIndex].priority)
                {
                    (elements[pi], elements[minIndex]) = (elements[minIndex], elements[pi]);
                    pi = minIndex;
                }
                else break;
            }
            return front.item;
        }
    }
}
