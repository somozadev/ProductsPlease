using UnityEngine;

namespace ProductsPlease.Interactions
{
    public class TriggerProbe : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[Probe] Enter: {other.name}");
        }

        void OnTriggerStay(Collider other)
        {
            Debug.Log($"[Probe] Stay: {other.name}");
        }

        void OnTriggerExit(Collider other)
        {
            Debug.Log($"[Probe] Exit: {other.name}");
        }

        void OnCollisionEnter(Collision c)
        {
            Debug.Log($"[Probe] CollEnter: {c.collider.name}");
        }

        void OnCollisionStay(Collision c)
        {
            Debug.Log($"[Probe] CollStay: {c.collider.name}");
        }

        void OnCollisionExit(Collision c)
        {
            Debug.Log($"[Probe] CollExit: {c.collider.name}");
        }
    }
}