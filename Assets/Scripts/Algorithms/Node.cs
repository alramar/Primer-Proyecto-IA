using UnityEngine;
using Assets.Scripts.Algorithms;
using System.Collections.Generic;
using System.Collections;
using Unity.Collections;
using Unity.VisualScripting;
namespace Assets.Scripts.Algorithms
{
    [RequireComponent(typeof(SphereCollider))]

    public class Node : MonoBehaviour
    {
        [SerializeField]
        public List<Node> neighbours;
        Collider collider;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            collider = GetComponent<SphereCollider>();
        }

        // Update is called once per frame
        void Update()
        {

        }

        void OnTriggerEnter(Collider other)
        {
            Debug.Log(other.name + " entered " + name);
        }
        public float Cost(Node neighbour)
        {
            if (neighbours.Contains(neighbour)) //Checks if it's a neighbour 
            { 
                return Vector3.Distance(transform.position, neighbour.transform.position);
            }
            return float.PositiveInfinity; // If not a neighbour put an enormous weight/cost
        }
    }


}


