using ProductsPlease.Managers;
using UnityEngine;
using UnityEngine.Events;

namespace ProductsPlease.Interactions
{
    public class InteractableComponent : MonoBehaviour
    {
        private Outline outline;
        private string initialMessage;
        public string message;
        public UnityEvent OnInteract;
        public UnityEvent OnEndInteract;

        public virtual void Initialise(GameObject owner)
        {
            outline = owner.GetComponent<Outline>();
            DisableOutline();
        }

        public virtual void StartInteract()
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
            if (GameManager.Instance.UIManager.toolTip)
                GameManager.Instance.UIManager.toolTip.text = "";
        }

        public void EnableOutline()
        {
            outline.enabled = true;
            if (GameManager.Instance.UIManager.toolTip)
                GameManager.Instance.UIManager.toolTip.text = message;
        }
    }
}