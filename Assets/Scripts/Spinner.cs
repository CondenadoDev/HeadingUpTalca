using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Spinner : NetworkBehaviour
{
    public Rigidbody rb;
    public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
    public Vector3 axis = Vector3.forward;
    public float speedFactor = 1;
    [Range(0,1)] public float phaseOffset = 0;

    // OPTIMIZATION: Cache expensive calculations
    private float lastCalculatedTime = -1f;
    private Quaternion lastRotation;
    private Vector3 transformedAxis;
    private float curveEndTime;
    private const float ROTATION_UPDATE_THRESHOLD = 0.02f;

    private void Start()
    {
        // OPTIMIZATION: Pre-calculate static values
        transformedAxis = transform.TransformDirection(axis);
        curveEndTime = curve.keys[curve.keys.Length - 1].time;
    }

    public override void FixedUpdateNetwork()
    {
        float currentTime = GameManager.Time * speedFactor + phaseOffset / curveEndTime;
		
        // OPTIMIZATION: Skip if time hasn't changed significantly
        if (Mathf.Abs(currentTime - lastCalculatedTime) < ROTATION_UPDATE_THRESHOLD)
            return;

        lastCalculatedTime = currentTime;
        float angle = curve.Evaluate(currentTime) * 360;
        Quaternion newRotation = Quaternion.AngleAxis(angle, transformedAxis);
		
        // OPTIMIZATION: Only rotate if rotation changed significantly
        if (Quaternion.Angle(newRotation, lastRotation) > 0.1f)
        {
            rb.MoveRotation(newRotation);
            lastRotation = newRotation;
        }
    }

    private void OnValidate()
    {
        if (rb != null)
        {
            transformedAxis = transform.TransformDirection(axis);
            curveEndTime = curve.keys.Length > 0 ? curve.keys[curve.keys.Length - 1].time : 1f;
            rb.transform.rotation = Quaternion.AngleAxis(
                curve.Evaluate(phaseOffset / curveEndTime) * 360,
                transformedAxis);
        }
    }
}