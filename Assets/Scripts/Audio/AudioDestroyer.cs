using UnityEngine;

public class AudioDestroyer : MonoBehaviour
{
    private AudioSource src;
    
    // OPTIMIZATION: Cache para evitar GetComponent cada frame
    private bool componentCached = false;
    
    // OPTIMIZATION: Usar WaitForSeconds en lugar de checks cada frame
    private float checkInterval = 0.1f; // Check cada 0.1 segundos en lugar de cada frame
    private float nextCheckTime = 0f;

    private void Awake()
    {
        src = GetComponent<AudioSource>();
        componentCached = src != null;
    }

    private void Update()
    {
        // OPTIMIZATION: Solo check en intervalos específicos
        if (Time.time < nextCheckTime) return;
        nextCheckTime = Time.time + checkInterval;

        // OPTIMIZATION: Evitar GetComponent si ya está cacheado
        if (!componentCached)
        {
            src = GetComponent<AudioSource>();
            componentCached = src != null;
        }

        if (src != null)
        {
            // OPTIMIZATION: Simplificar la condición de destrucción
            if (!src.isPlaying || (src.clip != null && src.timeSamples >= src.clip.samples))
            {
                Destroy(gameObject);
            }
        }
        else
        {
            // Si no hay AudioSource, destruir inmediatamente
            Destroy(gameObject);
        }
    }
}