using System;
using UnityEngine;

public class EventTriggerer : MonoBehaviour
{
    public event Action EventToTrigger;
    public event Action<bool> BooleanEventToTrigger;

    public void TriggerEvent() => EventToTrigger?.Invoke();

    public void TriggerEvent(bool value) => BooleanEventToTrigger?.Invoke(value);
}