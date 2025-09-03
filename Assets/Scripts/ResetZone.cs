using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ResetZone : MonoBehaviour
{
	public bool useEffect;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Putter player) && GameManager.Instance.Runner.IsServer)
        {
            var playerObj = player.PlayerObj;
            playerObj.Lives--;

            if (playerObj.Lives <= 0)
            {
                Debug.Log($"{playerObj.Nickname} lost their last life!");
                GameManager.PlayerDNF(playerObj);
            }
            else
            {
                // Broadcast life update to the client
                player.Rpc_UpdateLives(playerObj.Lives);

                // Respawn the player
                player.Rpc_Respawn(useEffect);
                Debug.Log($"{playerObj.Nickname} lost a life!");
            }
        }
    }

}