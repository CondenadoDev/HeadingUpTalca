using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableElementUI : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private float maxRadius = 100f; // Radio máximo de movimiento
    [SerializeField] private InteractionAreaUI[] interactionAreas;
    [SerializeField] private EventTriggerer notifier;

    private int selectedOption = -1;

    private RectTransform rectTransform;

    private Vector2 originalPosition;

    public EventTriggerer Notifier { get => notifier; }

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        if (canvas == null)
        {
            Debug.LogError("DraggableElement: No se encontró un Canvas padre.");
            return;
        }

        originalPosition = rectTransform.anchoredPosition;
    }

    void OnEnable()
    {
        foreach (InteractionAreaUI interaction in interactionAreas)
        {
            interaction.OnElementEnter += HandleElementEnter;
            interaction.OnElementExit += HandleElementExit;
        }
    }

    void OnDisable()
    {
        foreach (InteractionAreaUI interaction in interactionAreas)
        {
            interaction.OnElementEnter -= HandleElementEnter;
            interaction.OnElementExit -= HandleElementExit;
        }
    }

    private void HandleElementEnter(InteractionAreaUI interaction)
    {
        print("Ejecutando acción al ENTRAR del área de interacción " + interaction.AssignedOption + ".");
    }

    private void HandleElementExit(InteractionAreaUI interaction)
    {
        print("Ejecutando acción al SALIR del área de interacción " + interaction.AssignedOption + ".");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Puede usarse para efectos visuales
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        // Convierte la posición del mouse a coordenadas locales del RectTransform
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            canvas.worldCamera,
            out Vector2 localPoint
        );

        // Calcula la distancia desde la posición original
        Vector2 direction = localPoint - originalPosition;
        float distance = direction.magnitude;

        if (distance > maxRadius)
            localPoint = originalPosition + direction.normalized * maxRadius;

        rectTransform.anchoredPosition = localPoint;

        foreach (InteractionAreaUI interaction in interactionAreas)
            interaction.CheckOverlap(rectTransform);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition = originalPosition;
        //if (selectedOption != -1) notifier.TriggerEvent();
        selectedOption = -1;
    }
}
