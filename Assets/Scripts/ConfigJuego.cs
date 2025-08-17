using UnityEngine;

/// Config global elegida en el menú
public static class ConfigJuego
{
    public enum Modo { Normal, Practica, Seguro, Hardcore }
    public static Modo modo = Modo.Normal;

    // Reutiliza el enum del GameInitializer
    public static GameInitializer.AmbienteEscena ambiente = GameInitializer.AmbienteEscena.Bosque;

    // Extras
    public static int pelotasMortales = 6;       // usado en Normal/Hardcore
    public static float multiplicadorVelIA = 1f; // 1 = normal; 0 = quieta; >1 = más rápida
}
