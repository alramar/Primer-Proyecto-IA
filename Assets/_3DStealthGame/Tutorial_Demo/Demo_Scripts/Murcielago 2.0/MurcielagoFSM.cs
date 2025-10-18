using UnityEngine;

public class MurcielagoFSM
{
    private enum Estado { Patrulla, Persecución }
    private Estado estadoActual;
    private Murcielago murcielago;

    public MurcielagoFSM(Murcielago murcielago)
    {
        this.murcielago = murcielago;
        estadoActual = Estado.Patrulla;
    }

    public void ActualizarEstado()
    {
        switch (estadoActual)
        {
            case Estado.Patrulla:
                Patrullar();
                break;
            case Estado.Persecución:
                Perseguir();
                break;
        }
    }

    private void CambiarEstado(Estado nuevoEstado)
    {
        estadoActual = nuevoEstado;
    }

    private void Patrullar()
    {
        // Lógica de patrulla
    }
    
    private void Perseguir()
    {
        // Lógica de persecución
    }
}
