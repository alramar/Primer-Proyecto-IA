using UnityEngine;

public class BatEnemy : MonoBehaviour
{
    
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
