using UnityEngine;
using System.Collections.Generic;

public class NodoGrafoMurcielago : MonoBehaviour
{
    [System.Serializable]
    public class Arco
    {
        public NodoGrafoMurcielago vecino;

        [SerializeReference]
        public Arco siguiente;

        public Arco(NodoGrafoMurcielago vecino, Arco siguiente)
        {
            this.vecino = vecino;
            this.siguiente = siguiente;
        }
    }

    public Arco primerArco;
    public int grado;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.1f);

        Gizmos.color = Color.red;
        for (Arco arco = primerArco; arco != null; arco = arco.siguiente)
        {
            if (arco.vecino != null)
                Gizmos.DrawLine(transform.position, arco.vecino.transform.position);
        }
    }

    public void Conectar(NodoGrafoMurcielago otro)
    {
        if (otro == null || otro == this) return;

        for (Arco arco = primerArco; arco != null; arco = arco.siguiente)
        {
            if (arco.vecino == otro)
                return;
        }

        primerArco = new Arco(otro, primerArco);
        grado++;
    }
}
