using NUnit.Framework.Constraints;
using UnityEngine;

public class MaterialToggleHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MeshRenderer meshRenderer;

    [Header("Config")]
    [SerializeField] private int targetMaterialIndex;
    [SerializeField] private MaterialRenderMode initialRenderingMode;
    [SerializeField] private float targetOpacity;

    [Header("Test")]
    [SerializeField] private bool toggleMaterial;
    [SerializeField] private bool setOpaque;
    [SerializeField] private bool setTransparent;

    private Material material;

    private MaterialRenderMode currentRenderingMode;

    void Start()
    {
        InitializeRenderMode();
        material = meshRenderer.materials[targetMaterialIndex];
    }

    void Update()
    {
        if (toggleMaterial)
        {
            bool isRenderingOpaque = currentRenderingMode == MaterialRenderMode.Opaque;

            if (!isRenderingOpaque) SetOpaque();
            else SetTransparent();

            toggleMaterial = false;
        }
        if (setOpaque)
        {
            SetOpaque();
            setOpaque = false;
        }
        if (setTransparent)
        {
            SetTransparent();
            setTransparent = false;
        }
    }

    private void InitializeRenderMode() => SetRenderMode(initialRenderingMode);

    public void SetRenderMode(MaterialRenderMode mode)
    {
        if (mode == MaterialRenderMode.Opaque) SetOpaque();
        else SetTransparent();
    }

    private void SetOpaque()
    {
        if (material == null) return;

        material.DisableKeyword("_ALPHABLEND_ON");

        print("opaco");

        currentRenderingMode = MaterialRenderMode.Opaque;

        Color color = material.color;
        color.a = 1;
        material.color = color;

        material.SetOverrideTag("RenderType", "Opaque");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        material.SetInt("_ZWrite", 1);
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;

        material.SetShaderPassEnabled("ShadowCaster", true);

        // Asegurar la reasignación del material
        meshRenderer.materials[targetMaterialIndex] = material;
    }

    private void SetTransparent()
    {
        if (material == null) return;

        material.EnableKeyword("_ALPHABLEND_ON");

        print("transparente");

        currentRenderingMode = MaterialRenderMode.Transparent;

        Color color = material.color;
        color.a = targetOpacity;
        material.color = color;

        print(material.color.a);

        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        material.SetShaderPassEnabled("ShadowCaster", false);

        // Asegurar la reasignación del material
        meshRenderer.materials[targetMaterialIndex] = material;
    }

}