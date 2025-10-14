using System.Collections.Generic;
using UnityEngine;

public class NodeGraphGenerator : MonoBehaviour
{
    [Header("Configuración")]
    public string walkableTag = "Suelo";
    public string enemyTag = "Enemigo";
    public string playerTag = "Player";
    public string obstacleTag = "Obstaculo";
    public float spacing = 1f;
    [Tooltip("Altura sobre el suelo para posicionar el nodo y evitar colisiones")]
    public float nodeHeightOffset = 0.15f;
    [Tooltip("Altura desde la que se lanzan raycasts para ajustar nodos y conexiones")]
    public float raycastStartHeight = 3f;
    [Tooltip("Distancia mínima a la que un nodo puede estar de un obstáculo")]
    public float minDistanceToObstacle = 0.3f;

    [Header("Resultados")]
    public List<Node> nodes = new List<Node>();

    private void Awake()
    {
        GenerateNodes();
    }

    public void GenerateNodes()
    {
        nodes.Clear();
        Dictionary<Vector2Int, Node> nodeGrid = new Dictionary<Vector2Int, Node>();

        // 🔹 Desactivar temporalmente Players y Enemigos
        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        foreach (var p in players) p.SetActive(false);
        foreach (var e in enemies) e.SetActive(false);

        GameObject[] walkableObjects = GameObject.FindGameObjectsWithTag(walkableTag);

        // 1️⃣ Crear todos los nodos dentro de los bounds de los objetos "Suelo"
        foreach (GameObject obj in walkableObjects)
        {
            Collider col = obj.GetComponent<Collider>();
            if (col == null) continue;

            Bounds bounds = col.bounds;
            int minX = Mathf.FloorToInt(bounds.min.x / spacing);
            int maxX = Mathf.FloorToInt(bounds.max.x / spacing);
            int minZ = Mathf.FloorToInt(bounds.min.z / spacing);
            int maxZ = Mathf.FloorToInt(bounds.max.z / spacing);

            for (int gx = minX; gx <= maxX; gx++)
            {
                for (int gz = minZ; gz <= maxZ; gz++)
                {
                    Vector3 nodePos = new Vector3(gx * spacing, bounds.max.y + raycastStartHeight, gz * spacing);
                    Vector2Int gridPos = new Vector2Int(gx, gz);

                    if (!nodeGrid.ContainsKey(gridPos))
                    {
                        Node node = new Node(nodePos);
                        nodes.Add(node);
                        nodeGrid[gridPos] = node;
                    }
                }
            }
        }

        // 2️⃣ Ajustar altura y eliminar nodos sobre o cerca de obstáculos
        List<Node> nodesToRemove = new List<Node>();
        foreach (Node node in nodes)
        {
            if (Physics.Raycast(node.position, Vector3.down, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Ignore))
            {
                // Eliminar si está sobre un obstáculo
                if (hit.collider.CompareTag(obstacleTag))
                {
                    nodesToRemove.Add(node);
                    continue;
                }

                // Ajustar altura si es suelo o enemigo
                if (hit.collider.CompareTag(walkableTag) || hit.collider.CompareTag(enemyTag))
                    node.position = hit.point + Vector3.up * nodeHeightOffset;
                else
                {
                    nodesToRemove.Add(node);
                    continue;
                }

                // Comprobar distancia mínima a obstáculos cercanos
                Collider[] closeColliders = Physics.OverlapSphere(node.position, minDistanceToObstacle);
                foreach (Collider c in closeColliders)
                {
                    if (c.CompareTag(obstacleTag))
                    {
                        nodesToRemove.Add(node);
                        break;
                    }
                }
            }
            else
            {
                nodesToRemove.Add(node);
            }
        }

        // Eliminar nodos inválidos de la lista y del grid
        foreach (Node n in nodesToRemove)
        {
            nodes.Remove(n);
            Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(n.position.x / spacing),
                                                Mathf.RoundToInt(n.position.z / spacing));
            nodeGrid.Remove(gridPos);
        }

        // 3️⃣ Conectar nodos vecinos, verificando obstáculos entre ellos
        ConnectNodes(nodeGrid);

        // 4️⃣ Eliminar nodos aislados
        RemoveIsolatedNodes();

        // 🔹 Volver a activar Players y Enemigos
        foreach (var p in players) p.SetActive(true);
        foreach (var e in enemies) e.SetActive(true);

        Debug.Log("✅ Nodos generados: " + nodes.Count);
    }

    private void ConnectNodes(Dictionary<Vector2Int, Node> nodeGrid)
    {
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1,0), new Vector2Int(-1,0),
            new Vector2Int(0,1), new Vector2Int(0,-1),
            new Vector2Int(1,1), new Vector2Int(-1,1),
            new Vector2Int(1,-1), new Vector2Int(-1,-1)
        };

        foreach (var kvp in nodeGrid)
        {
            Node node = kvp.Value;

            foreach (var dir in directions)
            {
                Vector2Int neighborPos = kvp.Key + dir;
                if (nodeGrid.TryGetValue(neighborPos, out Node neighbor))
                {
                    Vector3 dirVec = neighbor.position - node.position;

                    // Raycast entre nodos para comprobar obstáculos
                    if (!Physics.Raycast(node.position + Vector3.up * nodeHeightOffset,
                                         dirVec.normalized,
                                         out RaycastHit hit,
                                         dirVec.magnitude,
                                         ~0,
                                         QueryTriggerInteraction.Ignore) ||
                        hit.collider.CompareTag(walkableTag) ||
                        hit.collider.CompareTag(enemyTag))
                    {
                        if (!node.neighbours.Contains(neighbor))
                            node.neighbours.Add(neighbor);
                    }
                }
            }
        }
    }

    private void RemoveIsolatedNodes()
    {
        nodes.RemoveAll(n => n.neighbours.Count == 0);
    }

    private void OnDrawGizmos()
    {
        if (nodes == null) return;

        Gizmos.color = Color.green;
        foreach (Node node in nodes)
        {
            Gizmos.DrawSphere(node.position, spacing * 0.15f);

            Gizmos.color = Color.cyan;
            foreach (Node neighbour in node.neighbours)
                Gizmos.DrawLine(node.position, neighbour.position);
        }
    }
}
