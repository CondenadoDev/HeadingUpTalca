using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class MoleCollision : NetworkBehaviour
{
    public float hitCooldown = 0.1f; // Cooldown before the same player can hit again
    public float minImpactSpeed = 2f; // Minimum player speed to count as a hit
    public MoleBehavior moleMovement;
    private Dictionary<PlayerRef, float> hitCooldowns = new Dictionary<PlayerRef, float>();

    private void OnCollisionEnter(Collision collision)
    {
        if (!Object.HasStateAuthority || moleMovement == null) return;

        if (collision.gameObject.TryGetComponent(out Putter player))
        {
            Debug.Log("Got a viable collision");
            float impactSpeed = collision.relativeVelocity.magnitude;
            PlayerRef playerRef = player.Object.InputAuthority;

            Debug.Log("Impact Speed: " + impactSpeed + " ; Minimum impact required: " + minImpactSpeed);

            if (impactSpeed >= minImpactSpeed)
            {
                if (!hitCooldowns.ContainsKey(playerRef) || Time.time > hitCooldowns[playerRef] + hitCooldown)
                {
                    Debug.Log("Not in cooldown, hitting!");
                    hitCooldowns[playerRef] = Time.time;
                    GameManager.Instance.Rpc_AddPoints(player, 1); // Sync points
                    AudioManager.Play("ballInHoleSFX", AudioManager.MixerTarget.SFX, player.transform.position);

                    // Reset the mole's behavior after it's hit
                    moleMovement.Rpc_ResetMole();
                }
                else
                {
                    Debug.Log("In cooldown!");
                }
            }
        }
    }
}