using System.Collections.Generic;
using UnityEngine;

// Espectro inherits from Enemy
public class Espectro : Enemy
{
    [Header("Espectro Specific Settings")]
    public List<Object> objects_to_possess;
    public float possession_range = 5.0f;
    public Transform player;
    public Node startNode;
    private Node currentNode;
    public new void Start()
    {
        currentNode = startNode;
    }

    // Update is called once per frame
    public new void Update()
    {

    }

    public void AStarRoute()
    {
        // Implementation for A* pathfinding
        List<Node> openSet = new List<Node>();
        List<Node> closedSet = new List<Node>();
        openSet.Add(currentNode);

        while (openSet.Count > 0)
        {
            closedSet.Add(currentNode);
            foreach (Node neighbor in currentNode.neighbors)
            {
                if (!closedSet.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                }
            }

            openSet.Remove(currentNode);
            // Sort by lowest cost by node
            openSet.Sort((a, b) => a.cost.CompareTo(b.cost));
            currentNode = openSet[0];
            
        }
    }
}