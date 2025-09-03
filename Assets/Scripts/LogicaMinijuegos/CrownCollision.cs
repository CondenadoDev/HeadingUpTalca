using Fusion;
using UnityEngine;

public class CrownCollision : NetworkBehaviour
{
    public Putter player;
    private float lastCollisionTime = 0f;
    public float collisionCooldown = 0.5f;

    public void Update()
    {
        // If the player currently has the crown, we increment their StackTime
        if (GameManager.Instance.GetCrownHolder() == player && GameManager.Time > 0f)
        {
            player.PlayerObj.StackedTime += Time.deltaTime;
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (!GameManager.Instance || !Runner.IsServer) return;
        if (Time.time < lastCollisionTime + collisionCooldown) return; // Prevents rapid switching

        if (other.gameObject.TryGetComponent(out Putter otherPlayer))
        {
            if (!GameManager.Instance.HasCrown(otherPlayer)) // Ensure the player doesn't already have the crown
            {
                // Call the RPC to update crown for all clients
                GameManager.Instance.Rpc_AssignCrown(otherPlayer.Object);

                lastCollisionTime = Time.time;

                // Apply cooldown to both players
                otherPlayer.crownCollision.SetCooldown(lastCollisionTime);
                player.crownCollision.SetCooldown(lastCollisionTime);
            }
        }
    }

    public void SetCooldown(float cooldown)
    {
        lastCollisionTime = cooldown;
    }
}