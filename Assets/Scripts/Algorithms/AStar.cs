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
        static float Heuristic(GenericNode a, GenericNode b)
        {
            Vector3 p1 = a.transform.position;
            Vector3 p2 = b.transform.position;
            return Math.Abs(p1.x - p2.x) + Math.Abs(p1.z - p2.z);
        }

        public static Dictionary<GenericNode, GenericNode> BreadthFirstSearch(GenericNode start, GenericNode goal, Dictionary<GenericNode, GenericNode> cameFrom, Dictionary<GenericNode, float> costSoFar)
        {
            PriorityQueue<GenericNode, float> frontier = new();
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
                GenericNode current = frontier.Dequeue();
                
                //DESCOMENTAR SI SE QUIERE PARAR AL ENCONTRAR EL NODO FINAL
                // if (current == goal)
                // {
                //     break;
                // }

                foreach (GenericNode neighbour in current.neighbours)
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

        public static List<GenericNode> ReconstructPath(GenericNode start, GenericNode goal, Dictionary<GenericNode, GenericNode> cameFrom)
        {
            List<GenericNode> path = new();
            GenericNode current = goal;
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
