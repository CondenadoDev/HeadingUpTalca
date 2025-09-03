using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BoostTrigger : MonoBehaviour
{
    public bool VelocityChanged = false;
	public float strength = 5f;
    public Vector3 direction = Vector3.up;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Putter player) && GameManager.Instance.Runner.IsServer)
        {
            // This is the current direction of the booster
            Vector3 boostDirection = transform.TransformDirection(direction).normalized;
            // Get the current speed of the ball
            float currentSpeed = player.rb.linearVelocity.magnitude;

            if (VelocityChanged)
            {
                player.rb.AddForce(boostDirection, ForceMode.VelocityChange);
            }
            else
            {
                // If the current speed is lower than the boost strength, set it to boost strength
                float newSpeed = Mathf.Max(currentSpeed, strength);

                // Apply the new velocity in the boost direction
                player.rb.linearVelocity = boostDirection * newSpeed;
            }
        }
    }
}