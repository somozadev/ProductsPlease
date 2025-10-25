using UnityEngine;
using UnityEngine.Serialization;

namespace ProductsPlease.Interactions
{
    [RequireComponent(typeof(Outline))]
    [RequireComponent(typeof(Collider))]
    public class Item : MonoBehaviour
    {
        private InteractableComponent interactableComponent;
        private void Awake()
        {
            interactableComponent = GetComponent<InteractableComponent>();
            interactableComponent.Initialise(gameObject);
        }
    }
}