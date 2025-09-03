using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GameOverCollider : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Putter player) && GameManager.Instance.Runner.IsServer)
        {
            GameManager.FinishPlayerState(player, instaLose: true);
        }
    }
}
