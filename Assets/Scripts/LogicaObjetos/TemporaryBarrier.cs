using Fusion;
using UnityEngine;

public class TemporaryBarrier : NetworkBehaviour
{
    public float duration = 5f;
    private float timer = 0f;
    private float fadeDuration = 1f;
    private float fadeTimer = 0f;

    private MeshRenderer barrierRenderer;
    private Material barrierMaterial;
    private Color originalColor;

    [Networked] public Color BarrierColor { get; set; }
    [Networked] public Vector3 NetworkedScale { get; set; }

    public override void Spawned()
    {
        // Apply color
        barrierRenderer = GetComponentInChildren<MeshRenderer>();
        if (barrierRenderer)
        {
            barrierMaterial = barrierRenderer.material;
            barrierMaterial.color = BarrierColor;
            originalColor = BarrierColor;
        }

        // Apply networked scale
        transform.localScale = NetworkedScale;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Runner.IsServer) return;

        timer += Runner.DeltaTime;

        // Start fading in the last second
        if (timer >= duration - fadeDuration && barrierMaterial)
        {
            fadeTimer += Runner.DeltaTime;
            float fadeFactor = Mathf.Clamp01(1f - (fadeTimer / fadeDuration));
            barrierMaterial.color = new Color(originalColor.r, originalColor.g, originalColor.b, fadeFactor);
        }

        // Destroy the barrier after the duration
        if (timer >= duration)
        {
            RPC_DestroyBarrier();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_DestroyBarrier()
    {
        Runner.Despawn(Object);
    }
}