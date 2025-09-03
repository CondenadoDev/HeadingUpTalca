using Fusion;
using System.Collections;
using UnityEngine;

public class MoleBehavior : NetworkBehaviour
{
    public float riseHeight = 1f; // How high the mole rises
    public float moveSpeed = 2f; // Speed of rising and sinking
    public Vector2 waitTimeRange = new Vector2(2f, 7f); // Time before rising
    public Vector2 surfaceTimeRange = new Vector2(2f, 4f); // Time before sinking

    private Vector3 startPos;
    private Vector3 targetPos;
    private float currentWaitTime; // Time before rising or sinking
    private bool isRising = false;
    private bool isMoving = false; // Track if the mole is moving

    public bool IsRising => isRising; // Expose isRising publicly

    public override void Spawned()
    {
        if (!Runner.IsServer) return;

        currentWaitTime = Random.Range(waitTimeRange.x, waitTimeRange.y);
        startPos = transform.position;
        targetPos = startPos + Vector3.up * riseHeight;

        // Start the cycle of movement and waiting
        StartCoroutine(MoleCycle());
    }

    // Coroutine that handles the rise and sink cycle with waiting times
    private IEnumerator MoleCycle()
    {
        while (true)
        {
            // Wait for the current wait time
            yield return new WaitForSeconds(currentWaitTime);

            // If the mole is not rising, start rising and set the timer
            if (!isRising && !isMoving)
            {
                isRising = true;
                isMoving = true; // Start moving
                Rpc_MoveMole(targetPos);
            }
            else if (isRising && !isMoving)
            {
                // If it's rising and reached the target, now it starts sinking
                isRising = false;
                isMoving = true; // Start moving again
                Rpc_MoveMole(startPos);
            }
        }
    }

    // Called each fixed update on the network (synchronized for all clients)
    public override void FixedUpdateNetwork()
    {
        if (!Runner.IsServer) return;

        // Move the mole only if it's moving
        if (isMoving)
        {
            if (isRising)
                Rpc_MoveMole(targetPos);
            else
                Rpc_MoveMole(startPos);
        }
    }

    // Method to handle the mole's movement and set the position directly
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_MoveMole(Vector3 destination)
    {
        float step = moveSpeed * Runner.DeltaTime;

        // Move the mole towards the target position
        Vector3 newPos = Vector3.MoveTowards(transform.position, destination, step);

        // Directly update the position of the transform (will sync across clients)
        transform.position = newPos;

        // If the mole reaches the destination, stop moving and start the waiting
        if (Vector3.Distance(newPos, destination) <= 0.01f)
        {
            isMoving = false; // Stop moving

            // If the mole reached the target position, set the appropriate wait time
            if (isRising)
            {
                currentWaitTime = Random.Range(surfaceTimeRange.x, surfaceTimeRange.y); // Wait time at top
            }
            else
            {
                currentWaitTime = Random.Range(waitTimeRange.x, waitTimeRange.y); // Wait time at bottom
            }

            // Start the next cycle (the timer for waiting starts only after it stops moving)
            StartCoroutine(MoleCycle());
        }
    }

    // RPC to reset the mole if hit by a player
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_ResetMole()
    {
        // Immediately stop the mole's routine and reset its position
        isRising = false;
        isMoving = true;
        currentWaitTime = Random.Range(waitTimeRange.x, waitTimeRange.y);
    }
}