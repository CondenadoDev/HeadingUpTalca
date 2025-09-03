using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class BulldozerMovement : NetworkBehaviour
{
    public float speed = 1f;
    private bool hasStarted = false;

    public override void FixedUpdateNetwork()
    {
        // Wait for GameManager.Time to start before spinning
        if (!hasStarted && GameManager.Time > 0)
        {
            hasStarted = true;
        }

        if (hasStarted) transform.Translate(Vector3.forward * speed * Runner.DeltaTime);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance.Runner.IsServer)
        {
            if (other.TryGetComponent(out Putter player))
            {
                GameManager.FinishPlayerState(player, instaLose: true);
            } 
        }
    }
}
