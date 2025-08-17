using UnityEngine;

public class CamaraSeguir : MonoBehaviour
{
    public Transform objetivo;
    public Vector3 offset = new Vector3(0, 12, -14);
    public float suavizado = 5f;

    void LateUpdate()
    {
        if (objetivo == null) return;
        Vector3 posDeseada = objetivo.position + offset;
        transform.position = Vector3.Lerp(transform.position, posDeseada, suavizado * Time.deltaTime);
        transform.LookAt(objetivo);
    }
}
