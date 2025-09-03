using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class NaoAnimatedObject : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] protected Animator animator;
    [SerializeField] protected GameObject targetObject;

    protected readonly int resetHash = Animator.StringToHash("Reset");
    private readonly Dictionary<string, int> hashes = new();

    public GameObject TargetObject { get => targetObject; }

    protected virtual void Awake() => InitializeHashes();

    public abstract void InitializeHashes();

    public void AddHash(string id, int value)
    {
        if (string.IsNullOrEmpty(id))
            throw new Exception("The id parameter can't be null or empty.");

        hashes.Add(id, value);
    }

    public void SetAnimation() => animator.SetTrigger(resetHash);

    public void SetAnimation(string id, bool value)
    {
        if (string.IsNullOrEmpty(id))
            throw new Exception("The id parameter can't be null or empty.");

        animator.SetTrigger(resetHash);
        animator.SetBool(hashes[id], value);
    }

    public void SetAnimation(string id, int value)
    {
        if (string.IsNullOrEmpty(id))
            throw new Exception("The id parameter can't be null or empty.");

        animator.SetTrigger(resetHash);
        animator.SetInteger(hashes[id], value);
    }

    public void SetAnimation(string id)
    {
        if (string.IsNullOrEmpty(id))
            throw new Exception("The id parameter can't be null or empty.");

        animator.SetTrigger(resetHash);
        animator.SetTrigger(hashes[id]);
    }

    public void SetParameter(string id, bool value)
    {
        if (string.IsNullOrEmpty(id))
            throw new Exception("The id parameter can't be null or empty.");

        animator.SetBool(hashes[id], value);
    }

    public void SetParameter(string id, int value)
    {
        if (string.IsNullOrEmpty(id))
            throw new Exception("The id parameter can't be null or empty.");

        animator.SetInteger(hashes[id], value);
    }

    public void SetParameter(string id)
    {
        if (string.IsNullOrEmpty(id))
            throw new Exception("The id parameter can't be null or empty.");

        animator.SetTrigger(hashes[id]);
    }
}
