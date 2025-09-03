using UnityEngine;
using Fusion;

public class BarrierGenerator : NetworkBehaviour
{
    public Putter player; // Reference to the player script
    public GameObject barrierPrefab; // Assign the barrier prefab in Inspector
    public float barrierSpawnRate = 0.5f;
    public float barrierOffset = 1f;
    public float rotationAdjustment = 180f;
    public float barrierScaleMultiplier = 0.1f;

    private bool hasStarted = false;
    private float lastBarrierTime = 0f;

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // Wait until the game has started
        if (!hasStarted && GameManager.Time > 0) hasStarted = true;

        // Get the player's velocity
        Vector3 velocity = player.rb.linearVelocity;

        // Check if the player is moving and spawn barriers at intervals
        if (hasStarted && velocity.magnitude > 0.1f && Runner.Tick - lastBarrierTime >= barrierSpawnRate)
        {
            RPC_SpawnBarrier(velocity);
            lastBarrierTime = Runner.Tick;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SpawnBarrier(Vector3 velocity)
    {
        // Calculate barrier position
        Vector3 barrierPosition = transform.position - velocity.normalized * barrierOffset;

        // Calculate barrier rotation
        Quaternion barrierRotation = Quaternion.LookRotation(-velocity.normalized, Vector3.up) * Quaternion.Euler(0, rotationAdjustment, 0);

        // Get player's color
        Color playerColor = Color.white;
        var playerRenderer = player.GetComponentInChildren<MeshRenderer>();
        if (playerRenderer)
        {
            playerColor = playerRenderer.material.color;
        }

        // Spawn the barrier with synchronized properties
        NetworkObject barrierInstance = GameManager.Instance.Runner.Spawn(barrierPrefab, barrierPosition, barrierRotation, null, (runner, obj) =>
        {
            var barrier = obj.GetComponent<TemporaryBarrier>();
            barrier.BarrierColor = playerColor;
            barrier.NetworkedScale = new Vector3(
                barrier.transform.localScale.x * player.transform.localScale.x * barrierScaleMultiplier,
                barrier.transform.localScale.y * player.transform.localScale.y * barrierScaleMultiplier,
                barrier.transform.localScale.z * player.transform.localScale.z * barrierScaleMultiplier);
        });
    }
}