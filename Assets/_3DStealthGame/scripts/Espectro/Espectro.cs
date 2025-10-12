using System.Collections.Generic;
using UnityEngine;

public class Espectro : Enemy
{
    [Header("Espectro Settings")]
    public float possessionRange = 5.0f;
    public float repathInterval = 1.0f;
    public Transform player;
    public Node startNode;
    
    private Node currentNode;
    private List<Node> path;
    private Node currentTargetNode;
    private float repathTimer = 0f;
    private bool isPossessing = false;
    private ParticleSystem currentParticleSystem;
    private Node lastPossessedNode; // Track del último nodo objeto poseído

    public new void Start()
    {
        base.Start();
        path = new List<Node>();
        currentNode = startNode != null ? startNode : FindClosestNode(transform.position);
        
        if (currentNode == null)
        {
            Debug.LogError("Espectro: No se pudo encontrar nodo inicial");
            enabled = false;
            return;
        }
        
        CalculateNewPath();
    }

    public new void Update()
    {
        base.Update();

        if (isPossessing) return;

        MoveAlongPath();

        repathTimer += Time.deltaTime;
        if (repathTimer >= repathInterval)
        {
            TryRecalculatePath();
            repathTimer = 0f;
        }
    }

    private void MoveAlongPath()
    {
        if (path == null || path.Count == 0) 
        {
            if (repathTimer >= repathInterval)
            {
                CalculateNewPath();
            }
            return;
        }

        Node targetNode = path[0];
        if (targetNode == null)
        {
            path.RemoveAt(0);
            return;
        }

        Vector3 direction = (targetNode.transform.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 90, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        if (Vector3.Distance(transform.position, targetNode.transform.position) < 0.1f)
        {
            Node previousNode = currentNode;
            currentNode = targetNode;
            path.RemoveAt(0);

            // Verificar si estamos saliendo de un nodo objeto hacia un nodo no-objeto
            if (previousNode != null && previousNode.isObject && 
                currentNode != null && !currentNode.isObject)
            {
                ShowModel(); // Reaparecer al moverse de objeto a no-objeto
            }

            if (CanPossessCurrentNode())
            {
                Possess();
            }
            else if (path.Count == 0)
            {
                CalculateNewPath();
            }
        }
    }

    private bool CanPossessCurrentNode()
    {
        return currentNode != null && 
               currentNode.isObject && 
               player != null &&
               Vector3.Distance(currentNode.transform.position, player.position) <= possessionRange;
    }

    private void Possess()
    {
        if (isPossessing || currentNode == null || !currentNode.isObject) return;
        
        isPossessing = true;
        lastPossessedNode = currentNode; // Guardar referencia al nodo poseído

        HideModel();
        PlayPossessionParticles();

        Invoke(nameof(ExitPossession), 3.0f);
    }

    private void ExitPossession()
    {
        StopPossessionParticles();
        
        // NO reaparecer aquí - esperar a moverse al siguiente nodo no-objeto
        isPossessing = false;
        
        // Buscar siguiente nodo desde la posición actual
        currentNode = FindClosestNode(transform.position);
        CalculateNewPath();
    }

    private void HideModel()
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            if (renderer != null)
                renderer.enabled = false;
        }

        foreach (var collider in GetComponentsInChildren<Collider>())
        {
            if (collider != null)
                collider.enabled = false;
        }
    }

    private void ShowModel()
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            if (renderer != null)
                renderer.enabled = true;
        }

        foreach (var collider in GetComponentsInChildren<Collider>())
        {
            if (collider != null)
                collider.enabled = true;
        }
    }

    private void ToggleModelVisibility(bool visible)
    {
        if (visible)
            ShowModel();
        else
            HideModel();
    }

    private void PlayPossessionParticles()
    {
        if (currentNode != null)
        {
            currentParticleSystem = currentNode.GetComponentInChildren<ParticleSystem>();
            if (currentParticleSystem != null)
            {
                currentParticleSystem.Play();
            }
        }
    }

    private void StopPossessionParticles()
    {
        if (currentParticleSystem != null)
        {
            currentParticleSystem.Stop();
            currentParticleSystem = null;
        }
    }

    private void TryRecalculatePath()
    {
        Node newTarget = FindClosestObjectToPlayer();
        if (newTarget != currentTargetNode || path == null || path.Count == 0)
        {
            CalculateNewPath();
        }
    }

    private void CalculateNewPath()
    {
        if (currentNode == null)
        {
            currentNode = FindClosestNode(transform.position);
            if (currentNode == null) return;
        }

        Node targetNode = FindClosestObjectToPlayer();
        if (targetNode == null) 
        {
            path?.Clear();
            currentTargetNode = null;
            return;
        }

        currentTargetNode = targetNode;
        path = AStarSearch(currentNode, targetNode);
        
        if (path == null)
            path = new List<Node>();
    }

    private List<Node> AStarSearch(Node start, Node goal)
    {
        if (start == null || goal == null) 
            return new List<Node>();

        var openSet = new List<Node> { start };
        var closedSet = new HashSet<Node>();
        var cameFrom = new Dictionary<Node, Node>();
        var gScore = new Dictionary<Node, float> { [start] = 0 };
        var fScore = new Dictionary<Node, float> { [start] = Heuristic(start, goal) };

        while (openSet.Count > 0)
        {
            Node current = GetLowestFScoreNode(openSet, fScore);
            if (current == null) break;

            if (current == goal)
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Node neighbor in current.neighbors)
            {
                if (neighbor == null || closedSet.Contains(neighbor)) 
                    continue;

                float tentativeG = gScore[current] + CalculateMoveCost(current, neighbor);

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

        return new List<Node>();
    }

    private Node GetLowestFScoreNode(List<Node> nodes, Dictionary<Node, float> fScore)
    {
        if (nodes == null || nodes.Count == 0) 
            return null;

        Node lowest = nodes[0];
        foreach (Node node in nodes)
        {
            if (node != null && 
                fScore.ContainsKey(node) && 
                fScore.ContainsKey(lowest) && 
                fScore[node] < fScore[lowest])
            {
                lowest = node;
            }
        }
        return lowest;
    }

    private float CalculateMoveCost(Node from, Node to)
    {
        if (from == null || to == null) 
            return float.MaxValue;

        float baseCost = Vector3.Distance(from.transform.position, to.transform.position);
        
        if (!to.isObject) 
            return baseCost;
        
        // Penalización mínima para variar rutas
        return baseCost * 1.1f;
    }

    private float Heuristic(Node a, Node b)
    {
        if (a == null || b == null) 
            return float.MaxValue;
            
        return Vector3.Distance(a.transform.position, b.transform.position);
    }

    private Node FindClosestObjectToPlayer()
    {
        if (player == null) 
            return null;

        Node[] allNodes = FindObjectsOfType<Node>();
        Node closest = null;
        float minDist = Mathf.Infinity;

        foreach (Node node in allNodes)
        {
            if (node == null || !node.isObject) 
                continue;
                
            float dist = Vector3.Distance(node.transform.position, player.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = node;
            }
        }
        
        return closest;
    }

    private Node FindClosestNode(Vector3 position)
    {
        Node[] allNodes = FindObjectsOfType<Node>();
        Node closest = null;
        float minDist = Mathf.Infinity;

        foreach (Node node in allNodes)
        {
            if (node == null) continue;
            
            float dist = Vector3.Distance(position, node.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = node;
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
            if (current != null)
                totalPath.Insert(0, current);
        }
        
        if (totalPath.Count > 0 && totalPath[0] == currentNode)
        {
            totalPath.RemoveAt(0);
        }
        
        return totalPath;
    }

    private void ResetPathfinding()
    {
        if (path != null)
            path.Clear();
            
        currentTargetNode = null;
        
        if (currentNode == null || Vector3.Distance(transform.position, currentNode.transform.position) > 2.0f)
        {
            currentNode = FindClosestNode(transform.position);
        }

        StopPossessionParticles();
        ShowModel(); // Asegurar que el modelo sea visible al resetear
    }

    public void ForceRecalculatePath()
    {
        ResetPathfinding();
        CalculateNewPath();
        repathTimer = 0f;
    }

    private void OnDrawGizmos()
    {
        if (path == null || path.Count == 0) 
            return;

        Gizmos.color = Color.red;
        for (int i = 0; i < path.Count - 1; i++)
        {
            if (path[i] != null && path[i + 1] != null)
            {
                Gizmos.DrawLine(path[i].transform.position, path[i + 1].transform.position);
            }
        }

        if (currentTargetNode != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentTargetNode.transform.position, 0.5f);
        }
    }
}