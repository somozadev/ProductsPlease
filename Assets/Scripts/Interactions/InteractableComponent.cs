using UnityEngine;
using UnityEngine.Events;

namespace ProductsPlease.Interactions
{
    public class InteractableComponent : MonoBehaviour
    {
        private Outline outline;
        public string message;
        public UnityEvent OnInteract;
        public UnityEvent OnEndInteract;

        public virtual void Initialise(GameObject owner)
        {
            outline = owner.GetComponent<Outline>();
            DisableOutline();
        }

        public virtual void Interact()
        {
            OnInteract?.Invoke();
        }

        public virtual void EndInteract()
        {
            OnEndInteract?.Invoke();
        }

        public void DisableOutline()
        {
            outline.enabled = false;
        }

        public void EnableOutline()
        {
            outline.enabled = true;
        }
    }
}