using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Fusion;

public class Cannon : NetworkBehaviour
{
    public float strength = 100;
    public float interval = 3;
    public float offset = 0;

    public Transform checkPos;
    public float checkRadius;

    // OPTIMIZATION: Cache para physics queries
    private Collider[] colliderBuffer = new Collider[10]; // Reuse array
    private List<Putter> putterCache = new List<Putter>(10);
	
    // OPTIMIZATION: Reduce frequency of expensive operations
    private int tickCounter = 0;
    private const int CHECK_INTERVAL = 3; // Every 3 ticks instead of every tick

    public override void FixedUpdateNetwork()
    {
        // OPTIMIZATION: Only check every N ticks
        tickCounter++;
        if (tickCounter < CHECK_INTERVAL) return;
        tickCounter = 0;

        if ((GameManager.Time + offset) % interval <= Runner.DeltaTime * CHECK_INTERVAL)
        {
            // OPTIMIZATION: Use NonAlloc version and cache putters
            int hitCount = Physics.OverlapSphereNonAlloc(checkPos.position, checkRadius, colliderBuffer);
			
            putterCache.Clear();
            for (int i = 0; i < hitCount; i++)
            {
                if (colliderBuffer[i].TryGetComponent(out Putter p))
                {
                    putterCache.Add(p);
                }
            }

            // Apply force to cached putters
            Vector3 force = checkPos.forward * strength;
            foreach (Putter putter in putterCache)
            {
                putter.rb.AddForce(force, ForceMode.VelocityChange);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (checkPos)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(checkPos.position, checkRadius);
        }
    }
}