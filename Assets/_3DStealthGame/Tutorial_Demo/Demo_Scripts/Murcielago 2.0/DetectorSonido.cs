using UnityEngine;
using UnityEngine.Assertions.Must;

public class DetectorSonido : MonoBehaviour
{

    public MurcielagoFSM fsm;
    public Collider playerCollider;

    private bool jugadorDentro = false;
    public float intervaloActualizacion;
    private float timer = 0f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Jugador detectado por el murciélago.");
            jugadorDentro = true;
            timer = 0f;
            fsm?.EmpezarPersecucionHacia(other.transform.position);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Jugador salió del área de detección.");
            jugadorDentro = false;
        }
    }

    private void Update()
    {
        if (!jugadorDentro) return;
        timer += Time.deltaTime;
        if (timer >= fsm.m.intervaloActualizacion)
        {
            timer = 0f;
            fsm?.ActualizarPosicionDetectada(playerCollider.transform.position);
        }
    }
}
