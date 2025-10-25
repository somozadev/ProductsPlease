using UnityEngine;
using UnityEngine.Events;

namespace ProductsPlease.Interactions
{
    
    [RequireComponent(typeof(Rigidbody))]
    public class DragableComponent : InteractableComponent
    {
        public float force = 600;
        public float damping = 6;
        public float distance = 15;
        
        
        private LineRenderer lr;
        private Transform lineRenderLocation;
        
        
        Transform jointTrans;
        float dragDepth;
        
        public override void Initialise(GameObject owner)
        {
            base.Initialise(owner);
        }

        public override void Interact()
        {
            base.Interact();
        }
        public override void EndInteract()
        {
            base.Interact();
        }

    }
}