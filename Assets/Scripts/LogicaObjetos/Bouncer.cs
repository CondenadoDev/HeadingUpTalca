using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Bouncer : MonoBehaviour
{
    public float bounceForce;
    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb != null)
        {
            Vector3 bounceDirection = Vector3.Reflect(collision.relativeVelocity.normalized, collision.GetContact(0).normal);
            rb.AddForce(bounceDirection * bounceForce, ForceMode.VelocityChange);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
