using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class HingeSpinnerController : NetworkBehaviour
{
    [Header("Spinner Settings")]
    public List<HingeJoint> hinges;
    public float startingSpeed = 10f;
    public float maxSpeed = 100f;
    public float accelerationDuration = 10f;
    
    private float speedIncreasePerSecond;
    private bool hasStarted = false;
    private float elapsedTime = 0f;

    // OPTIMIZATION: Cache motor settings
    private JointMotor[] cachedMotors;
    private float lastSpeed = -1f;

    public override void Spawned()
    {
        if (!Object.HasStateAuthority) return;
        
        speedIncreasePerSecond = (maxSpeed - startingSpeed) / accelerationDuration;

        // OPTIMIZATION: Pre-cache motor configurations
        cachedMotors = new JointMotor[hinges.Count];
        for (int i = 0; i < hinges.Count; i++)
        {
            cachedMotors[i] = hinges[i].motor;
            cachedMotors[i].force = 1000f;
            cachedMotors[i].freeSpin = false;
            cachedMotors[i].targetVelocity = 0f;
            
            hinges[i].motor = cachedMotors[i];
            hinges[i].useMotor = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        
        if (!hasStarted && GameManager.Time > 0)
        {
            hasStarted = true;
            elapsedTime = 0f;
        }

        if (hasStarted)
        {
            elapsedTime += Runner.DeltaTime;
            float newSpeed = Mathf.Clamp(startingSpeed + (speedIncreasePerSecond * elapsedTime), startingSpeed, maxSpeed);

            // OPTIMIZATION: Only update if speed changed significantly
            if (Mathf.Abs(newSpeed - lastSpeed) > 0.1f)
            {
                lastSpeed = newSpeed;
                for (int i = 0; i < hinges.Count; i++)
                {
                    cachedMotors[i].targetVelocity = newSpeed;
                    hinges[i].motor = cachedMotors[i];
                }
            }
        }
    }
}