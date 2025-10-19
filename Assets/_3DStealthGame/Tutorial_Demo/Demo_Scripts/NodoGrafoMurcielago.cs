using System.Collections.Generic;
using UnityEngine;

public class NodoGrafoMurcielago : MonoBehaviour
{
    [SerializeField] private List<NodoGrafoMurcielago> vecinos = new List<NodoGrafoMurcielago>();

    public IReadOnlyList<NodoGrafoMurcielago> Vecinos => vecinos;

    public void Conectar(NodoGrafoMurcielago otro)
    {
        if (otro == null || otro == this) return;
        if (!vecinos.Contains(otro))
        {
            vecinos.Add(otro);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.1f);

        Gizmos.color = Color.red;
        foreach (var v in vecinos)
        {
            if (v != null)
                Gizmos.DrawLine(transform.position, v.transform.position);
        }
    }
}
