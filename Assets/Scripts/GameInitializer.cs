using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class GameInitializer : MonoBehaviour
{
    public enum AmbienteEscena { Bosque, Desierto, Nieve, Volcanico, Nocturno }
    [Header("Ambiente")]
    public AmbienteEscena ambiente = AmbienteEscena.Bosque;

    [Header("Pelotas mortales")]
    public int cantidadPelotasMortales = 6;
    public Color colorPeligro = new Color(0.9f, 0.15f, 0.15f); // rojo

    private GameObject plano;
    private NavMeshSurface navMeshSurface;

    // Paleta
    private Color colorTerreno, colorMuro, colorComida, colorJugador, colorIA, colorFondo;

    void Start()
    {
        ConfigurarAmbienteVisual();

        CrearTerreno();
        // ❌ Ya NO llamo a CrearObstaculos();
        CrearParedesAlrededorDelTerreno();
        CrearJugadorSerpiente();
        CrearCulebraIAEchada();
        CrearComida();               // comida normal (verde)
        CrearPelotasMortales();      // ⚠️ comida mortal (roja)
        GenerarNavMesh();
    }

    // ---------- ambiente ----------
    void ConfigurarAmbienteVisual()
    {
        colorTerreno = new Color(0.30f, 0.70f, 0.30f);
        colorMuro    = new Color(0.55f, 0.55f, 0.55f);
        colorComida  = new Color(0.10f, 0.9f, 0.25f);
        colorJugador = new Color(0.12f, 0.55f, 1f);
        colorIA      = new Color(0.0f, 0.6f, 0.2f);
        colorFondo   = new Color(0.55f, 0.75f, 0.95f);

        var light = FindObjectOfType<Light>();
        if (light == null)
        {
            var go = new GameObject("Directional Light");
            light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
        }
        RenderSettings.fog = true;

        switch (ambiente)
        {
            case AmbienteEscena.Desierto:
                colorTerreno = new Color(0.91f, 0.82f, 0.58f);
                colorMuro    = new Color(0.70f, 0.65f, 0.55f);
                colorComida  = new Color(0.15f, 0.85f, 0.25f);
                colorJugador = new Color(0.10f, 0.55f, 0.95f);
                colorIA      = new Color(0.05f, 0.55f, 0.18f);
                colorFondo   = new Color(0.95f, 0.88f, 0.70f);
                light.color = new Color(1f, 0.95f, 0.85f);
                light.intensity = 1.2f;
                RenderSettings.ambientLight = new Color(0.9f, 0.85f, 0.75f);
                RenderSettings.fogColor = colorFondo;
                RenderSettings.fogDensity = 0.0035f;
                break;

            case AmbienteEscena.Nieve:
                colorTerreno = new Color(0.90f, 0.95f, 1f);
                colorMuro    = new Color(0.80f, 0.85f, 0.90f);
                colorComida  = new Color(0.20f, 0.70f, 0.95f);
                colorJugador = new Color(0.25f, 0.55f, 0.95f);
                colorIA      = new Color(0.15f, 0.45f, 0.80f);
                colorFondo   = new Color(0.80f, 0.90f, 1f);
                light.color = new Color(0.95f, 0.98f, 1f);
                light.intensity = 1.0f;
                RenderSettings.ambientLight = new Color(0.85f, 0.90f, 1f);
                RenderSettings.fogColor = colorFondo;
                RenderSettings.fogDensity = 0.0045f;
                break;

            case AmbienteEscena.Volcanico:
                colorTerreno = new Color(0.18f, 0.18f, 0.18f);
                colorMuro    = new Color(0.25f, 0.23f, 0.22f);
                colorComida  = new Color(1f, 0.5f, 0.1f);
                colorJugador = new Color(0.2f, 0.55f, 1f);
                colorIA      = new Color(0.85f, 0.1f, 0.1f);
                colorFondo   = new Color(0.08f, 0.07f, 0.07f);
                light.color = new Color(1f, 0.55f, 0.3f);
                light.intensity = 1.15f;
                RenderSettings.ambientLight = new Color(0.35f, 0.25f, 0.20f);
                RenderSettings.fogColor = new Color(0.12f, 0.07f, 0.06f);
                RenderSettings.fogDensity = 0.0065f;
                break;

            case AmbienteEscena.Nocturno:
                colorTerreno = new Color(0.08f, 0.13f, 0.18f);
                colorMuro    = new Color(0.18f, 0.20f, 0.22f);
                colorComida  = new Color(0.35f, 1f, 0.65f);
                colorJugador = new Color(0.30f, 0.70f, 1f);
                colorIA      = new Color(0.35f, 0.95f, 0.55f);
                colorFondo   = new Color(0.02f, 0.03f, 0.05f);
                light.color = new Color(0.55f, 0.65f, 1f);
                light.intensity = 0.6f;
                RenderSettings.ambientLight = new Color(0.12f, 0.16f, 0.22f);
                RenderSettings.fogColor = colorFondo;
                RenderSettings.fogDensity = 0.004f;
                break;
        }

        if (Camera.main != null) Camera.main.backgroundColor = colorFondo;
    }

    // ---------- creación ----------
    void CrearTerreno()
    {
        plano = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plano.name = "Terreno";
        plano.transform.localScale = new Vector3(5, 1, 5);
        plano.isStatic = true;

        plano.GetComponent<Renderer>().material.color = colorTerreno;
        if (!plano.TryGetComponent<MeshCollider>(out _)) plano.AddComponent<MeshCollider>();

        navMeshSurface = plano.AddComponent<NavMeshSurface>();
        navMeshSurface.collectObjects = CollectObjects.All;
    }

    void CrearJugadorSerpiente()
    {
        var jugador = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        jugador.name = "Jugador";
        jugador.tag = "Player";

        var cap = jugador.GetComponent<CapsuleCollider>();
        cap.direction = 2; cap.center = Vector3.zero; cap.height = 2.0f; cap.radius = 0.35f;

        jugador.transform.position = new Vector3(0, cap.radius, 0);
        jugador.transform.localScale = new Vector3(0.5f, 0.5f, 2.2f);
        jugador.GetComponent<Renderer>().material.color = colorJugador;

        var rb = jugador.AddComponent<Rigidbody>();
        rb.mass = 1.2f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        jugador.AddComponent<MovimientoJugador>();

        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(0, 12, -14);
            Camera.main.transform.LookAt(jugador.transform);
            var seg = Camera.main.gameObject.AddComponent<CamaraSeguir>();
            seg.objetivo = jugador.transform;
        }
    }

    void CrearCulebraIAEchada()
    {
        var culebra = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        culebra.name = "CulebraIA";

        var col = culebra.GetComponent<CapsuleCollider>();
        col.direction = 2; col.center = Vector3.zero; col.height = 2.2f; col.radius = 0.35f;

        culebra.transform.position = new Vector3(10, col.radius, 10);
        culebra.transform.localScale = new Vector3(0.5f, 0.5f, 2.4f);
        culebra.GetComponent<Renderer>().material.color = colorIA;

        var agent = culebra.AddComponent<NavMeshAgent>();
        agent.stoppingDistance = 0.8f;
        agent.acceleration     = 16f;
        agent.angularSpeed     = 240f;
        agent.radius           = 0.35f;
        agent.height           = 1.2f;
        agent.baseOffset       = col.radius;

        culebra.AddComponent<IACulebra>();
    }

    // Comida segura (verde)
    void CrearComida()
    {
        for (int i = 0; i < 15; i++)
        {
            var comida = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            comida.name = "Comida";

            var pos = new Vector3(Random.Range(-20, 20), 5f, Random.Range(-20, 20));
            if (Physics.Raycast(pos, Vector3.down, out var hit, 50f))
                pos = hit.point + Vector3.up * 0.25f;
            else
                pos = new Vector3(pos.x, 0.5f, pos.z);

            comida.transform.position = pos;
            comida.transform.localScale = Vector3.one * 0.5f;

            comida.GetComponent<Renderer>().material.color = colorComida;

            var sc = comida.GetComponent<SphereCollider>();
            sc.isTrigger = true;

            comida.AddComponent<Comida>();
        }
    }

    // ⚠️ Pelotas mortales (rojas): si el jugador las toca, muere
    void CrearPelotasMortales()
    {
        for (int i = 0; i < cantidadPelotasMortales; i++)
        {
            var deadly = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            deadly.name = "PelotaMortal";

            var pos = new Vector3(Random.Range(-22, 22), 5f, Random.Range(-22, 22));
            if (Physics.Raycast(pos, Vector3.down, out var hit, 50f))
                pos = hit.point + Vector3.up * 0.25f;
            else
                pos = new Vector3(pos.x, 0.5f, pos.z);

            deadly.transform.position = pos;
            deadly.transform.localScale = Vector3.one * 0.6f;

            var rend = deadly.GetComponent<Renderer>();
            rend.material.color = colorPeligro; // rojo

            var col = deadly.GetComponent<SphereCollider>();
            col.isTrigger = true;

            deadly.AddComponent<PelotaMortal>(); // 👈 nuevo script
        }
    }

    void CrearParedesAlrededorDelTerreno()
    {
        float t = 25f, h = 3f;

        void HacerPared(string nombre, Vector3 pos, Vector3 scale)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = nombre;
            p.transform.position = pos;
            p.transform.localScale = scale;
            p.isStatic = true;

            p.GetComponent<Renderer>().material.color = colorMuro;
            p.GetComponent<BoxCollider>().isTrigger = false;

            var nmo = p.AddComponent<NavMeshObstacle>();
            nmo.carving = true;
            nmo.shape = NavMeshObstacleShape.Box;
            nmo.size  = scale;
            nmo.center = Vector3.zero;
        }

        HacerPared("ParedSuperior",  new Vector3(0, h/2f,  t), new Vector3(t*2, h, 1));
        HacerPared("ParedInferior",  new Vector3(0, h/2f, -t), new Vector3(t*2, h, 1));
        HacerPared("ParedIzquierda", new Vector3(-t, h/2f, 0), new Vector3(1, h, t*2));
        HacerPared("ParedDerecha",   new Vector3( t, h/2f, 0), new Vector3(1, h, t*2));
    }

    void GenerarNavMesh()
    {
        navMeshSurface.BuildNavMesh();
    }
}
