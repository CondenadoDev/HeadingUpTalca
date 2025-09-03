using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    public Transform[] layers; // Capas del fondo
    public float[] parallaxScales; // Escala de movimiento por capa
    public float smoothness = 10f; // Suavidad del efecto

    private Vector3[] initialPositions;

    void Start()
    {
        // Guarda la posición inicial de cada capa
        initialPositions = new Vector3[layers.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            initialPositions[i] = layers[i].position;
        }
    }

    void Update()
    {
        // Evita división por cero
        if (Screen.width == 0 || Screen.height == 0) return;

        // Calcula la posición del mouse en valores de -0.5 a 0.5
        float mouseX = Mathf.Clamp((Input.mousePosition.x / Screen.width) - 0.5f, -0.5f, 0.5f);
        float mouseY = Mathf.Clamp((Input.mousePosition.y / Screen.height) - 0.5f, -0.5f, 0.5f);

        // Aplica el parallax a cada capa
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == null) continue; // Evita errores si falta una capa

            Vector3 targetPosition = initialPositions[i] + new Vector3(mouseX * parallaxScales[i], mouseY * parallaxScales[i], 0);
            layers[i].position = Vector3.Lerp(layers[i].position, targetPosition, Time.deltaTime * smoothness);
        }
    }
}
