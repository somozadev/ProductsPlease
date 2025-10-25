using UnityEngine;

namespace ProductsPlease.Player
{
    public abstract class PlayerComponent : MonoBehaviour
    {
        protected PlayerController Parent;
        protected bool IsEnabled;

        public virtual void Initialise()
        {
            Parent = FindPlayerControllerRecursive(transform);
            if (Parent == null)
                Debug.LogWarning("PlayerController is missing");
        }

        private PlayerController FindPlayerControllerRecursive(Transform currentTransform)
        {
            while (true)
            {
                if (currentTransform == null) return null;
                if (currentTransform.TryGetComponent(out PlayerController playerController)) return playerController;
                currentTransform = currentTransform.parent;
            }
        }

        public virtual void OnEnabled()
        {
            IsEnabled = true;
        }

        public virtual void OnDisabled()
        {
            IsEnabled = false;
        }
    }
}