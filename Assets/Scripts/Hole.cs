using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hole : MonoBehaviour
{
	private void OnTriggerStay(Collider other)
	{
		if (GameManager.Instance.Runner.IsServer)
		{
			if (other.TryGetComponent(out Putter player) && player.rb.linearVelocity.magnitude <= 0.5f)
			{
                GameManager.FinishPlayerState(player, victory: true);
			}
		}
	}
}
