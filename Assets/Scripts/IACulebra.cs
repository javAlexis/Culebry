using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering; // <- viene de tu script Perseguir

public class IACulebra : MonoBehaviour
{
    public enum Estado { Patrullando, Persiguiendo }
    public Estado estado = Estado.Patrullando;

    private Transform jugador;
    private NavMeshAgent agente;

    [Header("Velocidad (NavMesh)")]
    public float velocidadInicial = 2f;
    public float velocidadMax = 12f;
    private float velocidadActual;

    [Header("Visión")]
    public float rangoVision = 60f;
    public float fov = 80f;
    [SerializeField] LayerMask obstaculos = ~0;
    [SerializeField] float velGiro = 6f;

    [Header("Patrulla")]
    public float rangoPatrulla = 20f;

    // ---- Cola visual (solo estética) ----
    [Header("Cuerpo Culebra")]
    public int segmentosIniciales = 6;
    public float distanciaEntreSegmentos = 0.5f;
    public float amplitudOndulacion = 0.10f;
    public float frecuenciaOndulacion = 6f;
    public float suavizadoCola = 0.10f;
    private readonly List<Transform> segmentos = new List<Transform>();
    private Vector3[] velSeg;

    // Anti-stuck
    private float stuckTimer = 0f;
    private const float STUCK_TIME = 1.5f;
    private const float STUCK_VEL  = 0.05f;

    // Registro estático para reinicio
    private static readonly List<IACulebra> enemigos = new List<IACulebra>();
    private Vector3 spawnPos;
    private Quaternion spawnRot;

    void OnEnable()  { if (!enemigos.Contains(this)) enemigos.Add(this); }
    void OnDisable() { enemigos.Remove(this); }

    public static void ResetTodosLosEnemigos()
    {
        foreach (var e in enemigos) e.ResetEnemy();
    }

    // ====== BLOQUE "PERSEGUIR" (tal como lo enviaste) ======
    [Header("Perseguir (Kinemático, opcional)")]
    public bool usarPerseguirKinematico = false; // Activa tu lógica Perseguir
    public float velocidad = 5f;                 // <- nombre igual que tu script
    public Transform objetivo;                   // <- el jugador (se asigna en Start)
    private float x = 0f;
    private float y = 0f;
    private float z = 0f;
    // =======================================================

    void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        spawnPos = transform.position;
        spawnRot = transform.rotation;

        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
        {
            jugador = playerGO.transform;

            // vincula tu "objetivo" al jugador
            objetivo = jugador;
        }

        // conserva la Y inicial como en tu script Perseguir
        y = transform.position.y;

        velocidadActual = velocidadInicial;
        if (agente != null)
        {
            agente.speed = velocidadActual;
            agente.autoBraking = false;
            agente.autoRepath  = true;
            if (NavMesh.SamplePosition(transform.position, out var hit, 2f, NavMesh.AllAreas))
                agente.Warp(hit.position);
        }

        SeleccionarNuevoDestino();
        CrearCuerpoInicial();
    }

    void Update()
    {
        if (EstadoJuego.GameOver) { StopAgent(); return; }
        if ((jugador == null && objetivo == null) || agente == null && !usarPerseguirKinematico) return;

        bool visto = DetectarJugador();
        estado = visto ? Estado.Persiguiendo : Estado.Patrullando;

        // Logs tipo VerJugador
        if (visto) Debug.Log("Veo al jugador");
        else Debug.Log("No veo al jugador");

        if (estado == Estado.Persiguiendo)
        {
            if (usarPerseguirKinematico)
            {
                // Detén NavMesh si estaba activo
                if (agente != null)
                {
                    agente.isStopped = true;
                    agente.ResetPath();
                }
                // === Tu lógica "Perseguir" tal cual ===
                if (objetivo != null)
                {
                    x = transform.position.x;
                    z = transform.position.z;

                    if (objetivo.position.x > x) x += velocidad * Time.deltaTime;
                    if (objetivo.position.x < x) x -= velocidad * Time.deltaTime;

                    if (objetivo.position.z > z) z += velocidad * Time.deltaTime;
                    if (objetivo.position.z < z) z -= velocidad * Time.deltaTime;

                    transform.position = new Vector3(x, y, z);
                }
                // (Sin rotación a propósito: tu script no rotaba al enemigo)
            }
            else
            {
                // modo NavMesh (por defecto)
                if (agente != null)
                {
                    agente.isStopped = false;
                    agente.SetDestination(jugador.position);
                }
            }
        }
        else
        {
            // Patrulla solo con NavMesh; en kinemático no patrullamos
            if (!usarPerseguirKinematico)
                Patrullar();
        }

        // Anti-stuck (solo aplica a NavMesh)
        if (!usarPerseguirKinematico && agente != null &&
            !agente.pathPending && agente.velocity.magnitude < STUCK_VEL && agente.remainingDistance > 1.2f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > STUCK_TIME)
            {
                SeleccionarNuevoDestino();
                stuckTimer = 0f;
            }
        }
        else stuckTimer = 0f;

        ActualizarCola();
    }

    bool DetectarJugador()
    {
        var target = jugador != null ? jugador : objetivo;
        if (target == null) return false;

        Vector3 delta = target.position - transform.position;
        if (delta.sqrMagnitude > rangoVision * rangoVision) return false;

        Vector3 fwdXZ = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        Vector3 dirXZ = new Vector3(delta.x, 0f, delta.z).normalized;
        if (Vector3.Angle(fwdXZ, dirXZ) > fov) return false;

        Vector3 origen = transform.position + Vector3.up * 1.2f;
        Vector3 destino = target.position + Vector3.up * 1.2f;

        // En modo kinemático también respetamos la línea de visión
        if (Physics.Raycast(origen, (destino - origen).normalized, Vector3.Distance(origen, destino), obstaculos))
            return false;

        if (!usarPerseguirKinematico) // solo rotamos suave en NavMesh
        {
            if (dirXZ.sqrMagnitude > 0.0001f)
            {
                var rot = Quaternion.LookRotation(dirXZ, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, velGiro * Time.deltaTime);
            }
        }
        return true;
    }

    void Patrullar()
    {
        if (agente == null) return;
        if (!agente.hasPath || agente.remainingDistance < 1.5f)
            SeleccionarNuevoDestino();
    }

    void SeleccionarNuevoDestino()
    {
        if (agente == null) return;
        Vector3 p = new Vector3(Random.Range(-rangoPatrulla, rangoPatrulla), 0, Random.Range(-rangoPatrulla, rangoPatrulla));
        agente.SetDestination(p);
    }

    public void StopAgent()
    {
        if (agente == null) return;
        agente.isStopped = true;
        agente.ResetPath();
    }

    public void ResetEnemy()
    {
        if (agente == null && !usarPerseguirKinematico) return;
        transform.position = spawnPos;
        transform.rotation = spawnRot;

        if (agente != null)
        {
            agente.isStopped = false;
            agente.speed = velocidadInicial;
        }
        velocidadActual = velocidadInicial;
        stuckTimer = 0f;
        if (!usarPerseguirKinematico) SeleccionarNuevoDestino();
    }

    public void AumentarVelocidad(float delta)
    {
        velocidadActual = Mathf.Min(velocidadActual + delta, velocidadMax);
        if (agente != null) agente.speed = velocidadActual;
        // En kinemático, la velocidad usada es 'velocidad' (tu variable pública)
        // Puedes querer sincronizar así:
        // velocidad = Mathf.Min(velocidad + delta, velocidadMax);
    }

    // ===== Cola visual pegada al Terreno =====
    void CrearCuerpoInicial()
    {
        segmentos.Clear();
        velSeg = new Vector3[segmentosIniciales];

        Transform anterior = this.transform;
        for (int i = 0; i < segmentosIniciales; i++)
        {
            var seg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            seg.name = "IA_Segmento_" + i;
            seg.transform.localScale = Vector3.one * 0.7f;
            seg.transform.position = anterior.position - anterior.forward * distanciaEntreSegmentos;

            Destroy(seg.GetComponent<Collider>());
            seg.GetComponent<Renderer>().material.color = new Color(0f, 0.5f, 0.22f);
            seg.transform.SetParent(this.transform);

            seg.transform.position = SnapToGround(seg.transform.position, 0.35f);

            segmentos.Add(seg.transform);
            anterior = seg.transform;
        }
    }

    void ActualizarCola()
    {
        if (segmentos.Count == 0) return;

        Transform segPrev = this.transform;
        for (int i = 0; i < segmentos.Count; i++)
        {
            Vector3 objetivo = segPrev.position - segPrev.forward * distanciaEntreSegmentos;
            Vector3 lateral  = segPrev.right   * Mathf.Sin(Time.time * frecuenciaOndulacion + i * 0.6f) * amplitudOndulacion;

            Vector3 destino = SnapToGround(objetivo + lateral, 0.35f);

            Vector3 vel = velSeg[i];
            segmentos[i].position = Vector3.SmoothDamp(segmentos[i].position, destino, ref vel, suavizadoCola);
            velSeg[i] = vel;

            Vector3 dir = (segPrev.position - segmentos[i].position); dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                segmentos[i].rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

            segPrev = segmentos[i];
        }
    }

    // Prioriza el objeto llamado "Terreno"
    Vector3 SnapToGround(Vector3 pos, float yOffset)
    {
        Ray ray = new Ray(new Vector3(pos.x, 10f, pos.z), Vector3.down);
        var hits = Physics.RaycastAll(ray, 50f, ~0, QueryTriggerInteraction.Ignore);

        float? yTerreno = null;
        float minY = float.PositiveInfinity;

        foreach (var h in hits)
        {
            if (h.collider == null) continue;
            if (h.collider.gameObject.name == "Terreno") { yTerreno = h.point.y; break; }
            if (h.point.y < minY) minY = h.point.y;
        }

        float y = yTerreno.HasValue ? yTerreno.Value : (float.IsInfinity(minY) ? pos.y : minY);
        return new Vector3(pos.x, y + yOffset, pos.z);
    }
}
