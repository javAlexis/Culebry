using UnityEngine;

public class PelotaMortal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // ¿tocó el jugador?
        var pj = other.GetComponent<MovimientoJugador>();
        if (pj == null) pj = other.GetComponentInParent<MovimientoJugador>();
        if (pj == null) return;

        // Matar jugador
        pj.ForzarGameOver();

        // Efecto simple: destruir la pelota mortal
        Destroy(gameObject);
    }
}
