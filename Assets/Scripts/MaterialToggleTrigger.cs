using UnityEngine;

public class MaterialToggleTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out MaterialToggleHandler materialToggler))
        {
            print("choque con este wn");
            materialToggler.SetRenderMode(MaterialRenderMode.Transparent);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out MaterialToggleHandler materialToggler))
            materialToggler.SetRenderMode(MaterialRenderMode.Opaque);
    }
}