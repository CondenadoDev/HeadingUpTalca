using System.Collections.Generic;
using UnityEngine;
using Fusion;
public class HingeSpinnerController : NetworkBehaviour
{
    [Header("Spinner Settings")]
    public List<HingeJoint> hinges; // List of hinge joints to control
    public float startingSpeed = 10f; // Initial speed
    public float maxSpeed = 100f; // Maximum speed
    public float accelerationDuration = 10f; // Time to reach max speed
    
    private float speedIncreasePerSecond;
    private bool hasStarted = false;
    private float elapsedTime = 0f;

    public override void Spawned()
    {
        if (!Object.HasStateAuthority) return; // Only the server controls the physics
        
        // Calculate the rate of speed increase per second
        speedIncreasePerSecond = (maxSpeed - startingSpeed) / accelerationDuration;

        // Ensure all hinges start with the correct motor settings
        foreach (var hinge in hinges)
        {
            JointMotor motor = hinge.motor;
            motor.force = 1000f; // Ensure it has enough force to spin
            motor.targetVelocity = 0f; // Start stationary
            motor.freeSpin = false;
            hinge.motor = motor;
            hinge.useMotor = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        
        // Wait for GameManager.Time to start before spinning
        if (!hasStarted && GameManager.Time > 0)
        {
            hasStarted = true;
            elapsedTime = 0f; // Reset elapsed time
        }

        if (hasStarted)
        {
            elapsedTime += Runner.DeltaTime;
            float newSpeed = Mathf.Clamp(startingSpeed + (speedIncreasePerSecond * elapsedTime), startingSpeed, maxSpeed);

            foreach (var hinge in hinges)
            {
                JointMotor motor = hinge.motor;
                motor.targetVelocity = newSpeed;
                hinge.motor = motor;
            }
        }
    }
}
