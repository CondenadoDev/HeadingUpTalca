using Fusion;
using System.Collections;
using UnityEngine;

public class CrownHolder : NetworkBehaviour
{
    public Vector3 offset;

    [HideInInspector] public Putter putter;
    [HideInInspector] public Transform target;

    public void SetTarget(Putter owner)
    {
        putter = owner;
        target = putter?.interpolationTarget; // Safe null check
    }

    private void LateUpdate()
    {
        if (target)
        {
            transform.position = target.position + offset;
            transform.rotation = CameraController.Instance.transform.rotation;
        }
        else
        {
            StartCoroutine(WaitAndDestroy()); // Wait before destroying in case the target is reassigned
        }
    }

    private IEnumerator WaitAndDestroy()
    {
        yield return new WaitForSeconds(1);
        if (target != null && !target.Equals(null))
        {
            // continue following the target
            yield return null;
        }
        else // there has been no target to follow for 0.5 second so Destroy this:
        {
            Destroy(gameObject);
        }
    }
}
