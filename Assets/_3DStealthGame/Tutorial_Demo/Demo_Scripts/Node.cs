using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public Vector3 position;            // Posición del nodo en el mundo
    public List<Node> neighbours;       // Nodos vecinos conectados

    public Node(Vector3 pos)
    {
        position = pos;
        neighbours = new List<Node>();
    }
}