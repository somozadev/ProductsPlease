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

        public bool canBeUsed = true;

        public virtual void Initialise(GameObject owner)
        {
            outline = owner.GetComponent<Outline>();
            DisableOutline();
        }

        public virtual void StartInteract()
        {
            if (!canBeUsed) return;
            OnInteract?.Invoke();
            AudioManager.Instance.PlaySFX("Button");
        }

        public virtual void StartInteract(RaycastHit hit)
        {
            if (!canBeUsed) return;

            StartInteract();
        }

        public virtual void EndInteract()
        {
            if (!canBeUsed) return;

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
            if (!canBeUsed) return;

            outline.enabled = true;
            if (GameManager.Instance.UIManager.toolTip)
                GameManager.Instance.UIManager.toolTip.text = message;
        }
    }
}