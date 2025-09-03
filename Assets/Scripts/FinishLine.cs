using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinishLine : MonoBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		if (GameManager.Instance.Runner.IsServer)
		{
            if (other.TryGetComponent(out Putter player))
            {
                GameManager.FinishPlayerState(player, true);
            }
        }
	}
}
