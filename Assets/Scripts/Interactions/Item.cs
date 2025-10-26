using System;
using System.Collections.Generic;
using ProductsPlease.Managers;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProductsPlease.Interactions
{
    [RequireComponent(typeof(Outline))]
    [RequireComponent(typeof(Collider))]
    public class Item : MonoBehaviour
    {
        private InteractableComponent interactableComponent;
        public ItemData data;
    
        private void Awake()
        {
            interactableComponent = GetComponent<InteractableComponent>();
            interactableComponent.Initialise(gameObject);

        }

        public void Init(ItemData data)
        {
            this.data = data;
        }

        public void SetVisuals(Mesh mesh, Material material, bool addCollider = true, bool convex = false)
        {
            if (mesh == null)
                return;

            // MeshFilter
            var mf = gameObject.GetComponent<MeshFilter>();
            if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
#if UNITY_EDITOR
            mf.sharedMesh = mesh;
#else
            mf.mesh = mesh;
#endif

            // MeshRenderer
            var mr = gameObject.GetComponent<MeshRenderer>();
            if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();
            if (material != null)
            {
#if UNITY_EDITOR
                mr.sharedMaterial = material;
#else
                mr.material = material;
#endif
            }

            var mc = gameObject.GetComponent<MeshCollider>();
            if (mc == null) mc = gameObject.AddComponent<MeshCollider>();

            mc.sharedMesh = null;
            mc.sharedMesh = mesh;
            mc.convex = convex;
        }

        private void OnCollisionEnter(Collision other)
        {
            AudioManager.Instance.PlaySFX("Hit");
        }
    }
}