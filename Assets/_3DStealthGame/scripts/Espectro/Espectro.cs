using System.Collections.Generic;
using UnityEngine;
using GameEnding = StealthGame.GameEnding;

public class Espectro : Enemy
{
    [Header("Espectro Settings")]
    public float possessionRange = 5.0f;
    public float repathInterval = 1.0f;
    public float possessionDuration = 5.0f;
    public Transform player;
    public Node startNode;
    public GameEnding gameEnding;
    
    private Node currentNode;
    private List<Node> path;
    private Node currentTargetNode;
    private float repathTimer;
    private bool isPossessing;
    private ParticleSystem currentParticleSystem;
    private bool hasPlayedParticlesThisCycle;
    private Collider enemyCollider;

    public new void Start()
    {
        GetComponent<CapsuleCollider>().radius = detection_range;
        GetComponent<CapsuleCollider>().height = detection_range * 2;
        base.Start();
        
        enemyCollider = GetComponent<Collider>();
        path = new List<Node>();
        currentNode = startNode != null ? startNode : FindClosestNode();
        
        if (currentNode == null)
        {
            Debug.LogError("Espectro: No se pudo encontrar nodo inicial");
            enabled = false;
        }
        else
        {
            CalculateNewPath();
        }
    }

    public new void Update()
    {
        base.Update();

        repathTimer += Time.deltaTime;

        if (isPossessing)
        {
            if (repathTimer >= repathInterval)
            {
                CheckForBetterObject();
                repathTimer = 0f;
            }
            return;
        }

        MoveAlongPath();

        if (repathTimer >= repathInterval)
        {
            TryRecalculatePath();
            repathTimer = 0f;
        }
        Animate();
    }
    
   private void Animate()
   {
        // Usar un objeto hijo para la animación de flotación
        // Esto evita interferir con el movimiento principal
        float hoverHeight = 0.1f;
        float hoverSpeed = 3.0f;
        
        // Solo animar la posición local Y de un objeto hijo si existe
        // O crear un pequeño desplazamiento visual sin afectar la física
        float verticalOffset = Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
        
        // Aplicar offset mínimo que no afecte la colisión
        transform.position = new Vector3(transform.position.x, transform.position.y + verticalOffset * 0.1f, transform.position.z);
    }

    private void MoveAlongPath()
    {
        if (IsPathInvalid())
        {
            if (repathTimer >= repathInterval) CalculateNewPath();
            return;
        }

        Node targetNode = path[0];
        if (targetNode == null)
        {
            path.RemoveAt(0);
            return;
        }

        MoveTowardsTarget(targetNode);

        if (Vector3.Distance(transform.position, targetNode.transform.position) < 0.1f)
        {
            HandleNodeReached(targetNode);
        }
    }

    private bool IsPathInvalid() => path == null || path.Count == 0;

    private void MoveTowardsTarget(Node targetNode)
    {
        Vector3 direction = (targetNode.transform.position - transform.position).normalized;
        transform.position += direction * (speed * Time.deltaTime);

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, 
                Quaternion.LookRotation(direction) * Quaternion.Euler(0, 90, 0), 
                Time.deltaTime * 5f);
        }
    }

    private void HandleNodeReached(Node targetNode)
    {
        Node previousNode = currentNode;
        currentNode = targetNode;
        path.RemoveAt(0);

        // Salir de posesión al mover de objeto a no-objeto
        if (previousNode?.isObject == true && currentNode?.isObject == false)
        {
            ExitPossessionState();
        }

        // Poseer si es posible, sino recalcular camino
        if (CanPossessCurrentNode() && !hasPlayedParticlesThisCycle)
        {
            Possess();
        }
        else if (path.Count == 0)
        {
            CalculateNewPath();
        }
    }

    private bool CanPossessCurrentNode() => 
        currentNode?.isObject == true && 
        player != null &&
        Vector3.Distance(currentNode.transform.position, player.position) <= possessionRange;

    private void Possess()
    {
        if (isPossessing || currentNode?.isObject != true) return;
        
        isPossessing = true;
        hasPlayedParticlesThisCycle = true;

        SetCollider(true);
        HideModel();
        PlayPossessionParticles();
        Invoke(nameof(ExitPossession), possessionDuration);
    }

    private void CheckForBetterObject()
    {
        if (!isPossessing) return;

        Node betterTarget = FindClosestObjectToPlayer();
        if (betterTarget != null && betterTarget != currentNode)
        {
            CancelInvoke(nameof(ExitPossession));
            ExitPossession();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && isPossessing)
        {
            gameEnding?.CaughtPlayer();
        }
    }
    
    private void ExitPossession()
    {
        if (!isPossessing) return;

        CleanupPossession();
        currentNode = FindClosestNode();
        CalculateNewPath();
    }

    private void ExitPossessionState()
    {
        CleanupPossession();
        ShowModel();
    }

    private void CleanupPossession()
    {
        StopPossessionParticles();
        isPossessing = false;
        hasPlayedParticlesThisCycle = false;
        SetCollider(false);
    }

    private void SetCollider(bool enabled)
    {
        if (enemyCollider != null) enemyCollider.enabled = enabled;
    }

    private void HideModel()
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            if (renderer != null) renderer.enabled = false;
        }
    }

    private void ShowModel()
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            if (renderer != null) renderer.enabled = true;
        }
    }

    private void PlayPossessionParticles()
    {
        currentParticleSystem = currentNode?.GetComponentInChildren<ParticleSystem>();
        currentParticleSystem?.Play();
    }

    private void StopPossessionParticles()
    {
        currentParticleSystem?.Stop();
        currentParticleSystem = null;
    }

    private void TryRecalculatePath()
    {
        if (isPossessing) return;

        Node newTarget = FindClosestObjectToPlayer();
        if (newTarget != currentTargetNode || IsPathInvalid())
        {
            CalculateNewPath();
        }
    }

    private void CalculateNewPath()
    {
        if (currentNode == null)
        {
            currentNode = FindClosestNode();
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
        path = AStarSearch(currentNode, targetNode) ?? new List<Node>();
    }

    private List<Node> AStarSearch(Node start, Node goal)
    {
        if (start == null || goal == null) return new List<Node>();

        var openSet = new List<Node> { start };
        var closedSet = new HashSet<Node>();
        var cameFrom = new Dictionary<Node, Node>();
        var gScore = new Dictionary<Node, float> { [start] = 0 };
        var fScore = new Dictionary<Node, float> { [start] = Heuristic(start, goal) };

        while (openSet.Count > 0)
        {
            Node current = GetLowestFScoreNode(openSet, fScore);
            if (current == null) break;

            if (current == goal) return ReconstructPath(cameFrom, current);

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Node neighbor in current.neighbors)
            {
                if (neighbor == null || closedSet.Contains(neighbor)) continue;

                float tentativeG = gScore[current] + CalculateMoveCost(current, neighbor);

                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);

                    if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                }
            }
        }

        return new List<Node>();
    }

    private Node GetLowestFScoreNode(List<Node> nodes, Dictionary<Node, float> fScore)
    {
        if (nodes == null || nodes.Count == 0) return null;

        Node lowest = nodes[0];
        foreach (Node node in nodes)
        {
            if (node != null && fScore.TryGetValue(node, out float nodeScore) && 
                fScore.TryGetValue(lowest, out float lowestScore) && nodeScore < lowestScore)
            {
                lowest = node;
            }
        }
        return lowest;
    }

    private float CalculateMoveCost(Node from, Node to)
    {
        if (from == null || to == null) return float.MaxValue;

        float baseCost = Vector3.Distance(from.transform.position, to.transform.position);
        return to.isObject ? baseCost * 1.1f : baseCost;
    }

    private float Heuristic(Node a, Node b) => 
        (a == null || b == null) ? float.MaxValue : Vector3.Distance(a.transform.position, b.transform.position);

    private Node FindClosestObjectToPlayer()
    {
        if (player == null) return null;

        Node closest = null;
        float minDist = Mathf.Infinity;

        foreach (Node node in FindObjectsOfType<Node>())
        {
            if (node?.isObject != true) continue;
                
            float dist = Vector3.Distance(node.transform.position, player.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = node;
            }
        }
        
        return closest;
    }

    private Node FindClosestNode(Vector3 position = default)
    {
        if (position == default) position = transform.position;

        Node closest = null;
        float minDist = Mathf.Infinity;

        foreach (Node node in FindObjectsOfType<Node>())
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
        
        while (cameFrom.TryGetValue(current, out current) && current != null)
        {
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
        path?.Clear();
        currentTargetNode = null;
        
        if (currentNode == null || Vector3.Distance(transform.position, currentNode.transform.position) > 2.0f)
        {
            currentNode = FindClosestNode();
        }

        StopPossessionParticles();
        ShowModel();
        hasPlayedParticlesThisCycle = false;
        SetCollider(false);
    }

    public void ForceRecalculatePath()
    {
        ResetPathfinding();
        CalculateNewPath();
        repathTimer = 0f;
    }

    private void OnDrawGizmos()
    {
        if (path == null || path.Count == 0) return;

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