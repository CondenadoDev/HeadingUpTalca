using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class potato : MonoBehaviour
{
    public string playerTag = "Player"; //Tag jugador


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(playerTag))
        {
            Transform papa = collision.gameObject.transform.Find("Papa");
            if (papa != null)
            {
                Debug.Log("capturando ............................");
                capturar(papa);
            }
            else
            {
                Debug.Log("No ahi cabeza");
            }
        }
    }


    private void capturar(Transform papa)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null )
            { 
            rb.isKinematic = true;
            }
        transform.SetParent(papa);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }
}
