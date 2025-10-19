using UnityEngine;

public class DetectorSonido : MonoBehaviour
{
    public MurcielagoFSM fsm;
    public Collider playerCollider;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Jugador detectado");
            // Aquí podrías cambiar el estado de la FSM si quieres:
            // fsm.CambiarEstado(Estado.Persecucion);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Jugador perdido");
            // fsm.CambiarEstado(Estado.Patrulla);
        }
    }
}
