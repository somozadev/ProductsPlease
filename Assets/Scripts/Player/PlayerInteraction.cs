using System;
using ProductsPlease.Interactions;
using UnityEngine;

namespace ProductsPlease.Player
{
    public class PlayerInteraction : PlayerComponent
    {
        [SerializeField] private float playerReach = 3.0f;
        [SerializeField] private LayerMask raycastIgnoreLayers;
        private InteractableComponent currentInteractable;
        private Input.InputReader inputReader;
        private Camera cam;
        private RaycastHit currentHit;
        public override void Initialise()
        {
            base.Initialise();
            cam = Parent.Camera;
            inputReader = Parent.inputReader;
            currentInteractable = null;
        }

        public override void OnEnabled()
        {
            base.OnEnabled();
            inputReader.OnInteractEvent += TryInteract;
        }

        public override void OnDisabled()
        {
            base.OnDisabled();
            inputReader.OnInteractEvent -= TryInteract;
        }

        private void Update()
        {
            CheckInteraction();
        }

        private void TryInteract(bool start)
        {
            if (!currentInteractable) return;
            if (start)
                currentInteractable.StartInteract(currentHit);
            else
                currentInteractable.EndInteract();
        }

        private void CheckInteraction()
        {
            var camTransform = cam.transform;
            var ray = new Ray(camTransform.position, camTransform.forward);
            var layerMaskToUse = ~raycastIgnoreLayers;
            if (Physics.Raycast(ray, out var hit, playerReach, layerMaskToUse, QueryTriggerInteraction.Ignore))
            {
                currentHit = hit;
                if (hit.transform.gameObject.TryGetComponent(out InteractableComponent I))
                {
                    if (currentInteractable && I != currentInteractable)
                        currentInteractable.DisableOutline();

                    if (I.enabled)
                        SetNewCurrentInteractable(I);
                    else
                        UnsetCurrentInteractable();
                }
                else
                    UnsetCurrentInteractable();
            }
            else
                UnsetCurrentInteractable();
        }

        private void SetNewCurrentInteractable(InteractableComponent newInteractable)
        {
            currentInteractable = newInteractable;
            currentInteractable.EnableOutline();
        }

        private void UnsetCurrentInteractable()
        {
            if (!currentInteractable) return;
            currentInteractable.DisableOutline();
            currentInteractable = null;
        }
    }
}