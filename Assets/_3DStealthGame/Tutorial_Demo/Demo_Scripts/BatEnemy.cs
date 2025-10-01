using UnityEngine;

public class BatEnemy : MonoBehaviour
{
    private Transform transform;

    void Start()
    {
        transform = GetComponent<Transform>();
        transform.position -= new Vector3(0, 2.6f, 0);
    }

    void OnTriggerEnter (Collider other)
    {
        if (other.CompareTag("SoundCollider"))
        {
            Debug.Log("Jugador detectado por murcielago");
        }
    }

        void OnTriggerExit (Collider other)
        {
            
        }

    // Update is called once per frame
    void Update()
    {
        
    }
}
