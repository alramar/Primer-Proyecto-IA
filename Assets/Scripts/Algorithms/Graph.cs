using System.Collections.Generic;
using Assets.Scripts.Algorithms;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Algorithms
{
    public class Graph : MonoBehaviour
    {
        [SerializeField]
        GameObject mapObject;
        List<Transform> mapPlanes;
        [SerializeField]
        GameObject NodePrefab;
        Dictionary<Node, Node> res;
        List<Node> solution;
        [SerializeField]
        float scaleDivider = 0.5f;
        [SerializeField]
        float nodeSeparation = 3;
        float nodeSpace;
        [SerializeField]
        float nodeNeighbourRange = 4f;
        List<GameObject> generatedGONodes;
        [SerializeField]
        LayerMask layerMask;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            generatedGONodes = new();
            mapPlanes = new();

            GetMapFlooring();
            GenerateNodesOnMap();
            SetNeighboursByDistance();
            // Node start = generatedGONodes[Random.Range(0, generatedGONodes.Count - 1)].GetComponent<Node>();
            // Node goal = generatedGONodes[Random.Range(0, generatedGONodes.Count - 1)].GetComponent<Node>();
            // TryPathing(start.transform, goal.transform);

        }

        public List<Node> TryPathing(Transform startObject, Transform goalObject)
        {
            Dictionary<Node, Node> comeFrom = new();
            Dictionary<Node, float> costSoFar = new();
            Node start = GetClosestNode(startObject);
            Node goal = GetClosestNode(goalObject);
            res = new();
            res = AStar.BreadthFirstSearch(start, goal, comeFrom, costSoFar);
            solution = AStar.ReconstructPath(start, goal, res);
            return solution;
        }

        public List<Node> TryPathing(Transform startObject, Node goalNode)
        {
            Dictionary<Node, Node> comeFrom = new();
            Dictionary<Node, float> costSoFar = new();
            Node start = GetClosestNode(startObject);
            res = new();
            res = AStar.BreadthFirstSearch(start, goalNode, comeFrom, costSoFar);
            solution = AStar.ReconstructPath(start, goalNode, res);
            return solution;
        }

        public List<Node> TryPathing(Node startNode, Transform goalObject)
        {
            Dictionary<Node, Node> comeFrom = new();
            Dictionary<Node, float> costSoFar = new();
            Node goal = GetClosestNode(goalObject);
            res = new();
            res = AStar.BreadthFirstSearch(startNode, goal, comeFrom, costSoFar);
            solution = AStar.ReconstructPath(startNode, goal, res);
            return solution;
        }

        public List<Node> TryPathing(Node startNode, Node goalNode)
        {
            Dictionary<Node, Node> comeFrom = new();
            Dictionary<Node, float> costSoFar = new();
            res = new();
            res = AStar.BreadthFirstSearch(startNode, goalNode, comeFrom, costSoFar);
            solution = AStar.ReconstructPath(startNode, goalNode, res);
            return solution;
        }

        public Node GetClosestNode(Transform startObject, List<Node> excludedNodes = null)
        {
            excludedNodes ??= new();
            float minDistance = Vector3.Distance(generatedGONodes[0].transform.position, startObject.position);
            Node selection = generatedGONodes[0].GetComponent<Node>();
            foreach (GameObject nodeGO in generatedGONodes)
            {
                Node aux = nodeGO.GetComponent<Node>();
                float distance = Vector3.Distance(nodeGO.transform.position, startObject.position);
                Debug.Log("minDist: " + minDistance + " vs dist: " + distance);
                if (minDistance >= distance )//&& !excludedNodes.Contains(aux))
                {
                    minDistance = distance;
                    selection = nodeGO.GetComponent<Node>();
                }
            }
            return selection;
        }

        public Node GetFirstNodeInRadius(Transform startObject, float radius, List<Node> excludedNodes = null)
        {
            excludedNodes ??= new();
            foreach (GameObject nodeGO in generatedGONodes)
            {
                Node aux = nodeGO.GetComponent<Node>();
                float distance = Vector3.Distance(nodeGO.transform.position, startObject.position);
                if (radius >= distance )//&& !excludedNodes.Contains(aux))
                {
                    return aux;

                }
            }
            return null;
        }

        public Node GetANodeInRadius(Transform startObject, float radius, List<Node> excludedNodes = null)
        {
            excludedNodes ??= new();
            float i = 1f;
            float probability = i / generatedGONodes.Count;
            float random;
            foreach (GameObject nodeGO in generatedGONodes)
            {
                Node aux = nodeGO.GetComponent<Node>();
                random = Random.Range(0f, 1f);
                float distance = Vector3.Distance(nodeGO.transform.position, startObject.position);
                float ratio = distance / radius;
                probability += ratio;
                //Debug.Log("CHANCES " + random + " : " + probability);
                if (radius >= distance && random <= probability )//&& !excludedNodes.Contains(aux))
                {
                    return aux;

                }
                i++;
                probability = (float)(i / generatedGONodes.Count) + 0.2f;
            }
            return null;
        }


        public Node GetANodeInRange(Transform startObject, float minRadius, float maxRadius, List<Node> excludedNodes = null)
        {
            excludedNodes ??= new();

            float i = 1f;
            float probability = i / generatedGONodes.Count + 0.2f;
            float random;
            foreach (GameObject nodeGO in generatedGONodes)
            {
                Node aux = nodeGO.GetComponent<Node>();
                random = Random.Range(0f, 1f);
                float distance = Vector3.Distance(nodeGO.transform.position, startObject.position);
                if (maxRadius >= distance && minRadius <= distance && random < probability )//&& !excludedNodes.Contains(aux))
                {
                    return aux;

                }
                i++;
                probability = (float)(i / generatedGONodes.Count) + 0.2f;
            }
            return null;
        }

        public Node GetFurthestNodeInRadius(Transform startObject, float radius, List<Node> excludedNodes = null)
        {
            excludedNodes ??= new();

            Node maximumNode = null;
            float maxDistance = 0f;
            foreach (GameObject nodeGO in generatedGONodes)
            {
                Node aux = nodeGO.GetComponent<Node>();
                float distance = Vector3.Distance(startObject.position, nodeGO.transform.position);
                if (maxDistance < distance && radius >= distance)//&& !excludedNodes.Contains(aux))
                {
                    maxDistance = distance;
                    maximumNode = aux;
                }
            }
            return maximumNode;
        }


        void OnDrawGizmos()
        {
            if (!res.IsUnityNull())
            {
                foreach (KeyValuePair<Node, Node> pair in res)
                {
                    Gizmos.DrawLine(pair.Key.transform.position, pair.Value.transform.position);
                }
            }
            if (!solution.IsUnityNull())
            {
                Gizmos.color = Color.green;
                foreach (Node node in solution)
                {
                    Gizmos.DrawWireCube(node.transform.position, Vector3.one);
                }
            }

        }


        void GetMapFlooring()
        {
            foreach (Renderer child in mapObject.GetComponentsInChildren<Renderer>())
            {

                if (child.name.Contains("Plane", System.StringComparison.OrdinalIgnoreCase))
                {
                    mapPlanes.Add(child.transform);
                }


            }
        }
        void GenerateNodesOnMap()
        {
            nodeSpace = nodeSeparation * scaleDivider;
            foreach (Transform plane in mapPlanes)
            {
                Vector3 scale = GetWorldAlignedScale(plane.transform);

                GameObject aux = Instantiate(NodePrefab, plane.position + new Vector3(0, transform.position.y, 0), Quaternion.identity, transform);
                generatedGONodes.Add(aux);
                int nodesX = (int)(scale.x / scaleDivider);
                int nodesZ = (int)(scale.z / scaleDivider);
                for (int i = 1; i <= nodesX; i++)
                {
                    aux = Instantiate(NodePrefab, plane.position + new Vector3(nodeSpace * i, transform.position.y, 0), Quaternion.identity, transform);
                    generatedGONodes.Add(aux);
                    aux = Instantiate(NodePrefab, plane.position + new Vector3(-nodeSpace * i, transform.position.y, 0), Quaternion.identity, transform);
                    generatedGONodes.Add(aux);
                    for (int j = 1; j <= nodesZ; j++)
                    {
                        aux = Instantiate(NodePrefab, plane.position + new Vector3(0, transform.position.y, nodeSpace * j), Quaternion.identity, transform);
                        generatedGONodes.Add(aux);
                        aux = Instantiate(NodePrefab, plane.position + new Vector3(0, transform.position.y, -nodeSpace * j), Quaternion.identity, transform);
                        generatedGONodes.Add(aux);
                        aux = Instantiate(NodePrefab, plane.position + new Vector3(-nodeSpace * i, transform.position.y, -nodeSpace * j), Quaternion.identity, transform);
                        generatedGONodes.Add(aux);
                        aux = Instantiate(NodePrefab, plane.position + new Vector3(nodeSpace * i, transform.position.y, -nodeSpace * j), Quaternion.identity, transform);
                        generatedGONodes.Add(aux);
                        aux = Instantiate(NodePrefab, plane.position + new Vector3(nodeSpace * i, transform.position.y, nodeSpace * j), Quaternion.identity, transform);
                        generatedGONodes.Add(aux);
                        aux = Instantiate(NodePrefab, plane.position + new Vector3(-nodeSpace * i, transform.position.y, nodeSpace * j), Quaternion.identity, transform);
                        generatedGONodes.Add(aux);
                    }
                }
                if (nodesX < 1)
                {
                    for (int j = 1; j <= nodesZ; j++)
                    {
                        aux = Instantiate(NodePrefab, plane.position + new Vector3(0, transform.position.y, nodeSpace * j), Quaternion.identity, transform);
                        generatedGONodes.Add(aux);
                        aux = Instantiate(NodePrefab, plane.position + new Vector3(0, transform.position.y, -nodeSpace * j), Quaternion.identity, transform);
                        generatedGONodes.Add(aux);
                    }
                }
                // for (int i = 1; i <= nodesZ; i++)
                // {

                //     aux = Instantiate(NodePrefab, plane.position + new Vector3(0, 0, nodeSpace * i), Quaternion.identity, transform);
                //     generatedGONodes.Add(aux);
                //     aux = Instantiate(NodePrefab, plane.position + new Vector3(0, 0, -nodeSpace * i), Quaternion.identity, transform);
                //     generatedGONodes.Add(aux);

                // }
                // int smallerSide = (nodesX <= nodesZ) ? nodesX : nodesZ;
                // for (int i = 1; i <= smallerSide; i++)
                // {
                //     aux = Instantiate(NodePrefab, plane.position + new Vector3(nodeSpace * i, 0, nodeSpace * i), Quaternion.identity, transform);
                //     generatedGONodes.Add(aux);
                //     aux = Instantiate(NodePrefab, plane.position + new Vector3(-nodeSpace * i, 0, -nodeSpace * i), Quaternion.identity, transform);
                //     generatedGONodes.Add(aux);
                //     aux = Instantiate(NodePrefab, plane.position + new Vector3(-nodeSpace * i, 0, nodeSpace * i), Quaternion.identity, transform);
                //     generatedGONodes.Add(aux);
                //     aux = Instantiate(NodePrefab, plane.position + new Vector3(nodeSpace * i, 0, -nodeSpace * i), Quaternion.identity, transform);
                //     generatedGONodes.Add(aux);
                // }
            }
        }
        void SetNeighboursByDistance()
        {
            List<GameObject> visitedNodes = new();
            foreach (GameObject node in generatedGONodes)
            {
                Node aux = node.GetComponent<Node>();
                foreach (GameObject nodeOther in generatedGONodes)
                {
                    Vector3 a = node.transform.position;
                    Vector3 b = nodeOther.transform.position;
                    float distance = Vector3.Distance(a, b);
                    if (distance < nodeNeighbourRange)
                    {
                        RaycastHit hit;
                        if (Physics.Raycast(a, b - a, out hit, distance, layerMask))
                        {
                            //if (!visitedNodes.Contains(nodeOther))
                            //{F "+hit.col
                            Node correct = null;
                            if (hit.collider == null || hit.collider.TryGetComponent(out correct))
                            {
                                aux.neighbours.Add(correct);
                            }
                            //}
                        }
                    }
                }
                //visitedNodes.Add(node);
            }
        }

        Vector3 GetWorldAlignedScale(Transform t)
        {
            // Ejes locales escalados, expresados en espacio global
            Vector3 scaledRight = t.right * t.lossyScale.x;
            Vector3 scaledUp = t.up * t.lossyScale.y;
            Vector3 scaledForward = t.forward * t.lossyScale.z;

            // Magnitud proyectada sobre los ejes globales
            float globalX =
                Mathf.Abs(Vector3.Dot(scaledRight, Vector3.right)) +
                Mathf.Abs(Vector3.Dot(scaledUp, Vector3.right)) +
                Mathf.Abs(Vector3.Dot(scaledForward, Vector3.right));

            float globalY =
                Mathf.Abs(Vector3.Dot(scaledRight, Vector3.up)) +
                Mathf.Abs(Vector3.Dot(scaledUp, Vector3.up)) +
                Mathf.Abs(Vector3.Dot(scaledForward, Vector3.up));

            float globalZ =
                Mathf.Abs(Vector3.Dot(scaledRight, Vector3.forward)) +
                Mathf.Abs(Vector3.Dot(scaledUp, Vector3.forward)) +
                Mathf.Abs(Vector3.Dot(scaledForward, Vector3.forward));

            return new Vector3(globalX, globalY, globalZ);
        }

    }

}
