using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Estado global simple
public static class EstadoJuego
{
    public static bool GameOver = false;
}

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class MovimientoJugador : MonoBehaviour
{
    [Header("Movimiento cabeza")]
    public float velocidad = 6f;
    public float velocidadGiro = 160f;

    [Header("Cuerpo (cola)")]
    public int   segmentosIniciales      = 6;
    public float distanciaEntreSegmentos = 0.5f;
    public float amplitudOndulacion      = 0.08f;
    public float frecuenciaOndulacion    = 6f;
    public float suavizadoCola           = 0.10f;

    [Header("Crecimiento al comer")]
    public int segmentosPorComida = 2;

    private Rigidbody rb;
    private CapsuleCollider cap;
    private readonly List<Transform> segmentos = new List<Transform>();
    private readonly List<Vector3>   velSeg    = new List<Vector3>();
    private float hInput, vInput;

    // Game Over UI y respawn
    private Text msg;
    private Vector3 spawnPos;
    private Quaternion spawnRot;

    void Awake()
    {
        rb  = GetComponent<Rigidbody>();
        cap = GetComponent<CapsuleCollider>();

        // Collider acostado y CENTRADO
        cap.direction = 2;            // Z
        cap.center    = Vector3.zero; // centrado (evita bombeo)
        cap.height    = Mathf.Max(2.0f, cap.height);
        cap.radius    = Mathf.Max(0.35f, cap.radius);

        // Física estable
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Arrancar pegado al Terreno
        transform.position = SnapToGround(transform.position, cap.radius);

        spawnPos = transform.position;
        spawnRot = transform.rotation;

        CrearUI();
        CrearCuerpoInicial();
    }

    void CrearUI()
    {
        var canvasGO = new GameObject("Canvas_GameOver");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var textGO = new GameObject("Msg");
        textGO.transform.SetParent(canvasGO.transform);
        msg = textGO.AddComponent<Text>();
        msg.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        msg.fontSize = 32;
        msg.alignment = TextAnchor.MiddleCenter;
        msg.color = Color.white;
        var rt = msg.rectTransform;
        rt.sizeDelta = new Vector2(900, 220);
        rt.anchoredPosition = Vector2.zero;
        msg.gameObject.SetActive(false);
    }

    void Update()
    {
        if (EstadoJuego.GameOver)
        {
            hInput = vInput = 0f;
            if (Input.GetKeyDown(KeyCode.R)) Reiniciar();
            return;
        }

        vInput = Input.GetAxis("Vertical");
        hInput = Input.GetAxis("Horizontal");
    }

    void FixedUpdate()
    {
        if (EstadoJuego.GameOver) return;

        transform.Rotate(0f, hInput * velocidadGiro * Time.fixedDeltaTime, 0f);
        Vector3 avance = transform.forward * (vInput * velocidad * Time.fixedDeltaTime);
        rb.MovePosition(SnapToGround(rb.position + avance, cap.radius));

        ActualizarCola();
    }

    // ===== Cola con ondulación pegada SOLO al Terreno =====
    void CrearCuerpoInicial()
    {
        segmentos.Clear();
        velSeg.Clear();

        Transform anterior = this.transform;
        for (int i = 0; i < segmentosIniciales; i++)
        {
            var seg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            seg.name = "PJ_Segmento_" + i;
            seg.transform.localScale = Vector3.one * 0.7f;
            seg.transform.position = anterior.position - anterior.forward * distanciaEntreSegmentos;

            Destroy(seg.GetComponent<Collider>()); // solo visual
            seg.GetComponent<Renderer>().material.color = new Color(0.1f, 0.6f, 1f);
            seg.transform.SetParent(this.transform);

            seg.transform.position = SnapToGround(seg.transform.position, 0.35f);

            segmentos.Add(seg.transform);
            velSeg.Add(Vector3.zero);
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

    public void Crecer()
    {
        for (int s = 0; s < segmentosPorComida; s++) AgregarSegmento();
    }

    void AgregarSegmento()
    {
        Transform ultimo = (segmentos.Count == 0) ? this.transform : segmentos[segmentos.Count - 1];

        var seg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        seg.name = "PJ_Segmento_" + segmentos.Count;
        seg.transform.localScale = Vector3.one * 0.7f;
        seg.transform.position = SnapToGround(ultimo.position - ultimo.forward * distanciaEntreSegmentos, 0.35f);

        Destroy(seg.GetComponent<Collider>());
        seg.GetComponent<Renderer>().material.color = new Color(0.1f, 0.6f, 1f);
        seg.transform.SetParent(this.transform);

        segmentos.Add(seg.transform);
        velSeg.Add(Vector3.zero);
    }

    // ===== Eventos de muerte =====
    // Llamable desde otros scripts (PelotaMortal, etc.)
    public void ForzarGameOver()
    {
        if (!EstadoJuego.GameOver) TriggerGameOver();
    }

    // Colisión con la IA (no usamos tags)
    void OnCollisionEnter(Collision collision)
    {
        if (EstadoJuego.GameOver) return;
        var ia = collision.collider.GetComponent<IACulebra>();
        if (ia == null) ia = collision.collider.GetComponentInParent<IACulebra>();
        if (ia != null) TriggerGameOver();
    }

    void OnTriggerEnter(Collider other)
    {
        if (EstadoJuego.GameOver) return;
        var ia = other.GetComponent<IACulebra>();
        if (ia == null) ia = other.GetComponentInParent<IACulebra>();
        if (ia != null) TriggerGameOver();
    }

    // Activa el estado de Game Over, muestra UI y detiene todo
    void TriggerGameOver()
    {
        EstadoJuego.GameOver = true;

        if (msg != null)
        {
            msg.text = "¡GAME OVER!\nPresiona R para reiniciar";
            msg.gameObject.SetActive(true);
        }

        // parar inmediatamente al jugador
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // parar y resetear enemigos (dejan de moverse)
        IACulebra.ResetTodosLosEnemigos();
    }

    void Reiniciar()
    {
        EstadoJuego.GameOver = false;
        if (msg != null) msg.gameObject.SetActive(false);

        // reset jugador
        transform.position = spawnPos;
        transform.rotation = spawnRot;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // reset enemigos
        IACulebra.ResetTodosLosEnemigos();
    }

    // ===== Pegado al Terreno (prioriza el objeto llamado "Terreno") =====
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
