using UnityEngine;
using System.Collections;

public class Comida : MonoBehaviour
{
    private Renderer rend;
    private Color colorOriginal;

    [Header("Efecto visual")]
    public float tiempoDesvanecimiento = 1f;

    [Header("Aumento de velocidad enemigo")]
    public float incrementoVelocidadEnemigo = 0.5f;

    void Start()
    {
        rend = GetComponent<Renderer>();
        colorOriginal = rend.material.color;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Solo el jugador recoge
        var jugador = other.GetComponent<MovimientoJugador>();
        if (jugador == null) jugador = other.GetComponentInParent<MovimientoJugador>();
        if (jugador == null) return;

        jugador.Crecer();

        // Acelerar a las culebras IA (con null-check interno)
        var enemigos = FindObjectsOfType<IACulebra>();
        foreach (var e in enemigos) e.AumentarVelocidad(incrementoVelocidadEnemigo);

        StartCoroutine(FadeOut());
        Destroy(gameObject, tiempoDesvanecimiento);
    }

    private IEnumerator FadeOut()
    {
        float t = 0f;
        while (t < tiempoDesvanecimiento)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, t / tiempoDesvanecimiento);
            rend.material.color = new Color(colorOriginal.r, colorOriginal.g, colorOriginal.b, a);
            yield return null;
        }
    }
}
