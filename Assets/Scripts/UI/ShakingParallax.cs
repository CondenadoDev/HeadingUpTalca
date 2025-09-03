using UnityEngine;

public class ShakingParallax : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform canvas;
    [SerializeField] private Layer[] layers;

    [Header("Config")]
    [SerializeField] private float smoothness = 10f;
    [SerializeField] private float globalScaleParallax = 1;

    private Vector2[] initialPositions;

    [System.Serializable]
    struct Layer
    {
        public RectTransform layer;
        public float parallaxScale;
    }

    void Start()
    {
        initialPositions = new Vector2[layers.Length];
        for (int i = 0; i < layers.Length; i++)
            initialPositions[i] = layers[i].layer.anchoredPosition;
    }

    void Update() => Shaking();

    private void Shaking()
    {
        if (canvas == null || Screen.width == 0 || Screen.height == 0) return;

        // Convertimos la posición del mouse a coordenadas locales del Canvas
        Vector2 mousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, Input.mousePosition, null, out mousePosition);

        // Normalizamos el mouse en base al tamaño del Canvas
        float normalizedX = Mathf.Clamp(mousePosition.x / (canvas.rect.width * 0.5f), -0.5f, 0.5f);
        float normalizedY = Mathf.Clamp(mousePosition.y / (canvas.rect.height * 0.5f), -0.5f, 0.5f);

        // Aplicamos el efecto de parallax
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].layer == null) continue;

            float parallaxScale = layers[i].parallaxScale * globalScaleParallax;
            Vector2 targetPosition = initialPositions[i] + new Vector2(normalizedX * parallaxScale, normalizedY * parallaxScale);
            layers[i].layer.anchoredPosition = Vector2.Lerp(layers[i].layer.anchoredPosition, targetPosition, Time.deltaTime * smoothness);
        }
    }
}
