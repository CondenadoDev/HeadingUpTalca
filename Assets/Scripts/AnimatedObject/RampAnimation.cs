using UnityEngine;

public class RampAnimation : NaoAnimatedObject
{
    [Header("References")]
    [SerializeField] private EventTriggerer triggerer;

    void OnEnable() => triggerer.EventToTrigger += SetSimpleAnimation;

    void OnDisable() => triggerer.EventToTrigger -= SetSimpleAnimation;

    public override void InitializeHashes()
    {
    }

    public void SetSimpleAnimation() => SetAnimation();
}
