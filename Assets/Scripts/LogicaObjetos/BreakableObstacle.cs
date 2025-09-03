using Fusion;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BreakableObstacle : NetworkBehaviour
{
    [Header("Obstacle Settings")]
    public int maxHitPoints = 3;

    [Networked, OnChangedRender(nameof(StatChanged))]
    private int currentHitPoints { get; set; }

    [Header("Destruction Settings")]
    public float minImpactSpeed = 3f;  // Minimum speed required to damage

    public ParticleSystem hitEffect; // Effect for when hit

    private Collider obstacleCollider;
    private MeshRenderer obstacleRenderer;
    private bool isTriggerMode = false; // Track if the collider is a trigger

    public event System.Action OnStatChanged;

    public override void Spawned()
    {
        if (Object.HasStateAuthority) // Only the server initializes HP
        {
            currentHitPoints = maxHitPoints;
        }

        obstacleCollider = GetComponent<Collider>();
        obstacleRenderer = GetComponent<MeshRenderer>();
        obstacleCollider.isTrigger = false; // Start as a solid object
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!Object.HasStateAuthority || isTriggerMode) return; // Only server processes hits, unless it's in trigger mode

        if (collision.gameObject.TryGetComponent(out Putter player))
        {
            float impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed >= minImpactSpeed && !obstacleCollider.isTrigger)
            {
                RPC_TakeDamage(1);
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_TakeDamage(int damage)
    {
        if (!Object.HasStateAuthority || currentHitPoints <= 0) return;

        currentHitPoints -= damage;
        Debug.Log($"Obstacle hit! Remaining HP: {currentHitPoints}");
        PlayHitEffect();

        if (currentHitPoints == 1) RPC_EnableTriggerMode();
        else if (currentHitPoints <= 0) RPC_DestroyObstacle();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_EnableTriggerMode()
    {
        Debug.Log("Obstacle is now in trigger mode!");
        isTriggerMode = true;
        obstacleCollider.isTrigger = true; // Convert to trigger
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isTriggerMode) return; // Only process when in trigger mode

        if (other.gameObject.TryGetComponent(out Putter player))
        {
            float playerSpeed = player.rb.linearVelocity.magnitude;

            if (playerSpeed >= minImpactSpeed)
            {
                RPC_TakeDamage(1); // Take the final hit and destroy it
            }
            else
            {
                Debug.Log($"Player {player.Object.Id} is too slow! Obstacle reverts to solid temporarily.");
                obstacleCollider.isTrigger = false; // Revert back to solid
                isTriggerMode = false;

                // Re-enable trigger mode after a short delay
                Invoke(nameof(RestoreTriggerMode), 0.1f);
            }
        }
    }

    private void RestoreTriggerMode()
    {
        if (currentHitPoints == 1) // Only restore if it still has 1 HP
        {
            Debug.Log("Restoring trigger mode after short delay.");
            RPC_EnableTriggerMode();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_DestroyObstacle()
    {
        if (obstacleCollider != null) obstacleCollider.enabled = false; // Disable collider first
        if (obstacleRenderer != null) obstacleRenderer.enabled = false;

        Invoke(nameof(DestroySelf), hitEffect.main.duration + 0.5f); // Wait before removing
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }

    private void StatChanged()
    {
        OnStatChanged?.Invoke();
    }

    private void PlayHitEffect()
    {
        if (hitEffect != null)
        {
            hitEffect.Stop();
            hitEffect.Play();
        }
    }
}