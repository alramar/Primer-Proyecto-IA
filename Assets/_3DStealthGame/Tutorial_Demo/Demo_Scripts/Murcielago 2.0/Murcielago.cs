using UnityEngine;

public class Murcielago : MonoBehaviour
{
    private MurcielagoFSM fsm;
    private GameEnding gameEnding;
    private PlayerController playerController;
    private SphereCollider detectorSonido;
    
    [Header("Stats de Murciélago")]
    public float velocidad = 2f;
    public float radioDeteccionCaminar = 0.6f;
    public float radioDeteccionCorrer = 1.5f;


    void Start()
    {
        fsm = new MurcielagoFSM(this);
        gameEnding = FindFirstObjectByType<GameEnding>();
        playerController = FindFirstObjectByType<PlayerController>();
        detectorSonido = GetComponent<SphereCollider>();

        detectorSonido.radius = radioDeteccionCorrer;
    }

    void Update()
    {
        if(!playerController.isMoving) detectorSonido.radius = 0f;
        else detectorSonido.radius = playerController.isWalking ? radioDeteccionCaminar : radioDeteccionCorrer;
    }

    void FixedUpdate()
    {
        fsm.ActualizarEstado();
    }
}
