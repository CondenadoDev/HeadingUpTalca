using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Extender : NetworkBehaviour
{
    public AnimationCurve curve;
    public Rigidbody extenderRb;
    public float timeOffset = 0;

    // OPTIMIZATION: Cache calculation
    private Vector3 lastPosition;
    private float lastCalculatedTime = -1f;
    private const float POSITION_UPDATE_THRESHOLD = 0.02f; // Only update if time difference is significant

    public override void FixedUpdateNetwork()
    {
        float currentTime = GameManager.Time + timeOffset;
		
        // OPTIMIZATION: Skip calculation if time hasn't changed significantly
        if (Mathf.Abs(currentTime - lastCalculatedTime) < POSITION_UPDATE_THRESHOLD)
            return;

        lastCalculatedTime = currentTime;
        Vector3 newPosition = transform.TransformPoint(0, curve.Evaluate(currentTime), 0);
		
        // OPTIMIZATION: Only move if position changed significantly
        if (Vector3.Distance(newPosition, lastPosition) > 0.001f)
        {
            extenderRb.MovePosition(newPosition);
            lastPosition = newPosition;
        }
    }
}