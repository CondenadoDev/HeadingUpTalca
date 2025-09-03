using UnityEngine;

public class ImageVisibilityAnimation : NaoAnimatedObject
{
    [Header("References")]
    [SerializeField] private EventTriggerer triggerer;

    private readonly int visibleHash = Animator.StringToHash(Parameters.Visible.ToString());

    void OnEnable() => triggerer.BooleanEventToTrigger += SetAnimationVisible;

    void OnDisable() => triggerer.BooleanEventToTrigger -= SetAnimationVisible;

    enum Parameters
    {
        Visible
    }

    public override void InitializeHashes()
        => AddHash(Parameters.Visible.ToString(), visibleHash);

    public void SetAnimationVisible(bool visible)
    {
        print("me llamaron");
        SetAnimation(Parameters.Visible.ToString(), visible);
    }
}