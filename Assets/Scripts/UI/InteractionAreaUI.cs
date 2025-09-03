using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class InteractionAreaUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EventTriggerer togglerEvent;

    [Header("Config")]
    [SerializeField] private int assignedOption;

    private bool isInside = false;

    private RectTransform rectTransform;

    public event Action<InteractionAreaUI> OnElementEnter;
    public event Action<InteractionAreaUI> OnElementExit;

    public int AssignedOption { get => assignedOption; }

    void Start() => rectTransform = GetComponent<RectTransform>();

    public void CheckOverlap(RectTransform target)
    {
        bool isOverlapping = RectTransformUtility.RectangleContainsScreenPoint(rectTransform, target.position, null);

        if (isOverlapping && !isInside)
        {
            isInside = true;
            OnElementEnter?.Invoke(this);  // Notificar que el objeto ha entrado
            togglerEvent.TriggerEvent(true);
            Debug.Log("DraggableElement ha entrado en el área de interacción.");
        }
        else if (!isOverlapping && isInside)
        {
            isInside = false;
            OnElementExit?.Invoke(this);  // Notificar que el objeto ha salido
            togglerEvent.TriggerEvent(false);
            Debug.Log("DraggableElement ha salido del área de interacción.");
        }
    }

    public void TurnOffAnimation() => togglerEvent?.TriggerEvent(false);

    public bool IsOverlapping(RectTransform target)
    => RectTransformUtility.RectangleContainsScreenPoint(rectTransform, target.position, null);
}