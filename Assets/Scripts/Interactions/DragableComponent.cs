﻿using System;
using ProductsPlease.Player.Input;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace ProductsPlease.Interactions
{
    [RequireComponent(typeof(Rigidbody))]
    public class DragableComponent : InteractableComponent
    {
        public float force = 600;
        public float damping = 6;
        public float distance = 15;


        public LineRenderer lr;
        public Transform lineRenderLocation;

        private InputReader inputReader;

        Transform jointTrans;
        float dragDepth;

        private void Start()
        {
            if (!lineRenderLocation || !lr)
            {
                lr = FindAnyObjectByType<LineRenderer>(FindObjectsInactive.Include);
                lineRenderLocation = lr.transform;
            }
        }

        void OnMouseDown()
        {
            HandleInputBegin(Input.mousePosition);
        }

        void OnMouseUp()
        {
            HandleInputEnd(Input.mousePosition);
        }

        void OnMouseDrag()
        {
            HandleInput(Input.mousePosition);
        }

        public void HandleInputBegin(Vector3 screenPosition)
        {
            var ray = Camera.main.ScreenPointToRay(screenPosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, distance))
            {
                if (hit.transform.TryGetComponent(out DragableComponent dragableComponent))
                {
                    if (!dragableComponent) return;
                    dragDepth = CameraPlane.CameraToPointDepth(Camera.main, hit.point);
                    jointTrans = AttachJoint(hit.rigidbody, hit.point);
                }
            }

            lr.positionCount = 2;
        }

        public void HandleInput(Vector3 screenPosition)
        {
            if (jointTrans == null)
                return;
            var worldPos = Camera.main.ScreenToWorldPoint(screenPosition);
            jointTrans.position = CameraPlane.ScreenToWorldPlanePoint(Camera.main, dragDepth, screenPosition);

            DrawRope();
        }

        public void HandleInputEnd(Vector3 screenPosition)
        {
            DestroyRope();
            if (jointTrans.gameObject)
                Destroy(jointTrans.gameObject);
            

        }

        Transform AttachJoint(Rigidbody rb, Vector3 attachmentPosition)
        {
            
            
            GameObject go = new GameObject("Attachment Point");
            go.hideFlags = HideFlags.HideInHierarchy;
            go.transform.position = attachmentPosition;

   
            
            var newRb = go.AddComponent<Rigidbody>();
            newRb.isKinematic = true;

            
            var joint = go.AddComponent<ConfigurableJoint>();
            joint.connectedBody = rb;
            joint.configuredInWorldSpace = true;
            joint.xDrive = NewJointDrive(force, damping);
            joint.yDrive = NewJointDrive(force, damping);
            joint.zDrive = NewJointDrive(force, damping);
            joint.slerpDrive = NewJointDrive(force, damping);
            joint.rotationDriveMode = RotationDriveMode.Slerp;

            return go.transform;
        }

        private JointDrive NewJointDrive(float force, float damping)
        {
            JointDrive drive = new JointDrive();
#pragma warning disable CS0618 // Type or member is obsolete
            drive.mode = JointDriveMode.Position;
#pragma warning restore CS0618 // Type or member is obsolete
            drive.positionSpring = force;
            drive.positionDamper = damping;
            drive.maximumForce = Mathf.Infinity;
            return drive;
        }

        private void DrawRope()
        {
            if (jointTrans == null)
            {
                return;
            }

            lr.SetPosition(0, lineRenderLocation.position);
            lr.SetPosition(1, this.transform.position);
        }

        private void DestroyRope()
        {
            lr.positionCount = 0;
        }
    }
}