using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;


namespace Assets.Scripts.Algorithms
{
    public class AStar
    {
        static float Heuristic(Node a, Node b)
        {
            Vector3 p1 = a.transform.position;
            Vector3 p2 = b.transform.position;
            return Math.Abs(p1.x - p2.x) + Math.Abs(p1.z - p2.z);
        }

        public static Dictionary<Node, Node> BreadthFirstSearch(Node start, Node goal, Dictionary<Node, Node> cameFrom, Dictionary<Node, float> costSoFar)
        {
            PriorityQueue<Node, float> frontier = new();
            frontier.Enqueue(start, 0);
            cameFrom = new()
            {
                { start, start }
            };

            costSoFar = new()
            {
                { start, 0 }
            };

            while (frontier.Count > 0)
            {
                Node current = frontier.Dequeue();
                
                //DESCOMENTAR SI SE QUIERE PARAR AL ENCONTRAR EL NODO FINAL
                // if (current == goal)
                // {
                //     break;
                // }

                foreach (Node neighbour in current.neighbours)
                {
                    float newCost = costSoFar[current] + current.Cost(neighbour);
                    if (!costSoFar.ContainsKey(neighbour) || costSoFar[neighbour] > newCost)
                    {
                        frontier.Enqueue(neighbour, newCost + Heuristic(neighbour, goal));
                        costSoFar[neighbour] = newCost;
                        cameFrom[neighbour] = current;
                    }
                }
            }

            return cameFrom;
        }

        public static List<Node> ReconstructPath(Node start, Node goal, Dictionary<Node, Node> cameFrom)
        {
            List<Node> path = new();
            Node current = goal;
            if (!cameFrom.ContainsKey(goal))
            {
                return path;
            }
            while (!current.Equals(start))
            {
                path.Add(current);
                current = cameFrom[current];
            }
            path.Add(start);
            path.Reverse();
            return path;
        }
    }
    
}
