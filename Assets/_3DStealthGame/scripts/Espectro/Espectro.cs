using System.Collections.Generic;
using UnityEngine;

// Espectro inherits from Enemy
public class Espectro : Enemy
{
    [Header("Espectro Specific Settings")]
    public float possession_range = 5.0f;
    public float repathInterval = 1.0f;          // tiempo entre recalculaciones automáticas
    public Transform player;
    public Node startNode;
    private Node currentNode;
    private List<Node> path;
    private Node currentTargetNode;
    private float repathTimer = 0f;
    private bool isPossessing = false;

    [System.Obsolete]
    public new void Start()
    {
        path = new List<Node>();
        currentNode = startNode;
        AStarRoute();
    }

    [System.Obsolete]
    public new void Update()
    {
        if (isPossessing) return;

        // Mover por el camino
        MoveAlongPath();

        // --- Recalcular ruta si el jugador o el entorno cambian ---
        repathTimer += Time.deltaTime;
        if (repathTimer >= repathInterval)
        {
            Node newTarget = FindClosestObjectToPlayer();

            // Si el objetivo cambia (nuevo objeto más cercano o desaparece)
            if (newTarget != currentTargetNode)
            {
                AStarRoute();
            }
            repathTimer = 0f;
        }
    }

    private void MoveAlongPath()
    {
        if (path == null || path.Count == 0) return;

        Node targetNode = path[0];
        Vector3 direction = (targetNode.transform.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        // Rotación suave hacia el movimiento
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            // Corrige y 90 grados
            lookRotation *= Quaternion.Euler(0, 90, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        // Si llegó al nodo siguiente
        if (Vector3.Distance(transform.position, targetNode.transform.position) < 0.1f)
        {
            currentNode = targetNode;
            path.RemoveAt(0);

            // Si el nodo es poseíble y está cerca del jugador → posee
            if (currentNode.isObject && Vector3.Distance(currentNode.transform.position, player.position) <= possession_range)
            {
                Possess();
            }
        }
    }

    public void Possess()
    {
        if (!currentNode.isObject) return;
        isPossessing = true;

        // Ocultar modelo del espectro
        GetComponentInChildren<MeshRenderer>().enabled = false;

        // Activar partículas
        ParticleSystem ps = currentNode.GetComponentInChildren<ParticleSystem>();
        if (ps != null) ps.Play();

        // Esperar unos segundos y volver al modo normal
        Invoke(nameof(ExitPossession), 3.0f);
    }

    [System.Obsolete]
    private void ExitPossession()
    {
        // Detener partículas
        ParticleSystem ps = currentNode.GetComponentInChildren<ParticleSystem>();
        if (ps != null) ps.Stop();

        // Mostrar espectro nuevamente
        GetComponentInChildren<MeshRenderer>().enabled = true;

        isPossessing = false;

        // Buscar nuevo objeto más cercano al jugador
        ResetAStar();
        AStarRoute();
    }

    [System.Obsolete]
    public void AStarRoute()
    {
        path.Clear();
        currentNode = FindClosestNode(transform.position);

        Node targetNode = FindClosestObjectToPlayer();
        if (targetNode == null)
        {
            Debug.LogWarning("Espectro: No hay objetos poseíbles cerca del jugador.");
            return;
        }

        currentTargetNode = targetNode;

        // Calcular camino con A*
        path = AStarSearch(currentNode, targetNode);
    }

    // --- A* Algorithm ---
    private List<Node> AStarSearch(Node start, Node goal)
    {
        List<Node> openSet = new() { start };
        HashSet<Node> closedSet = new();
        Dictionary<Node, Node> cameFrom = new Dictionary<Node, Node>();
        Dictionary<Node, float> gScore = new Dictionary<Node, float> { [start] = 0 };
        Dictionary<Node, float> fScore = new Dictionary<Node, float> { [start] = Heuristic(start, goal) };

        while (openSet.Count > 0)
        {
            Node current = openSet[0];
            foreach (Node n in openSet)
                if (fScore[n] < fScore[current])
                    current = n;

            if (current == goal)
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Node neighbor in current.neighbors)
            {
                if (neighbor == null || closedSet.Contains(neighbor)) continue;

                float tentativeG = gScore[current] + Vector3.Distance(current.transform.position, neighbor.transform.position);
                if (neighbor.isObject)
                    tentativeG *= 0.8f; // favorece objetos

                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        return new List<Node>(); // Sin ruta encontrada
    }

    private float Heuristic(Node a, Node b)
    {
        // Distancia euclidiana
        return Vector3.Distance(a.transform.position, b.transform.position);
    }

    // Encuentra el nodo objeto más cercano al jugador
    [System.Obsolete]
    private Node FindClosestObjectToPlayer()
    {
        Node[] allNodes = FindObjectsOfType<Node>();
        Node closest = null;
        float minDist = Mathf.Infinity;

        foreach (Node n in allNodes)
        {
            if (!n.isObject) continue;
            float dist = Vector3.Distance(n.transform.position, player.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = n;
            }
        }

        return closest;
    }

    private List<Node> ReconstructPath(Dictionary<Node, Node> cameFrom, Node current)
    {
        List<Node> totalPath = new List<Node> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Insert(0, current);
        }

        return totalPath;
    }

    [System.Obsolete]
    private Node FindClosestNode(Vector3 position)
    {
        Node[] allNodes = FindObjectsOfType<Node>();
        Node closest = null;
        float minDist = Mathf.Infinity;

        foreach (Node n in allNodes)
        {
            if (n == null) continue;
            float dist = Vector3.Distance(position, n.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = n;
            }
        }

        if (closest == null)
        {
            Debug.LogWarning("[Espectro] No se encontró ningún nodo cercano a la posición.");
        }

        return closest;
    }

    [System.Obsolete]
    private void ResetAStar()
    {
        path.Clear();
        currentNode = FindClosestNode(transform.position);

        Node[] allNodes = FindObjectsOfType<Node>();
        foreach (Node n in allNodes)
        {
            n.cost = 0f;
            n.gScore = 0;
            n.fScore = 0;
            n.visited = false;
        }

        currentTargetNode = null;

        isPossessing = false;

        if (GetComponentInChildren<MeshRenderer>() != null)
            GetComponentInChildren<MeshRenderer>().enabled = true;

        if (currentNode != null && currentNode.GetComponentInChildren<ParticleSystem>() != null)
            currentNode.GetComponentInChildren<ParticleSystem>().Stop();
    }
}
