using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProductsPlease.Player
{
    public class PlayerLook : PlayerComponent
    {
        [SerializeField] private float xSensitivity = 3.5f;
        [SerializeField] private float ySensitivity = 3.5f;
        [SerializeField] private Transform lookPivot;
        private float xRotation = 0f;
        private float mouseX = 0f;
        private float mouseY = 0f;

        private Camera cam;
        private Input.InputReader inputReader;


        public override void Initialise()
        {
            base.Initialise();
            cam = Parent.Camera;
            inputReader = Parent.inputReader;
            if (!lookPivot)
                lookPivot = transform;
        }

        public override void OnEnabled()
        {
            base.OnEnabled();
            inputReader.OnLookEvent += ProcessLook;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        public override void OnDisabled()
        {
            base.OnDisabled();
            inputReader.OnLookEvent -= ProcessLook;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
        }

        private void LateUpdate()
        {
            Look();
        }

        private void ProcessLook(Vector2 input)
        {
            mouseX = input.x * xSensitivity;
            mouseY = input.y * ySensitivity;
            xRotation = Mathf.Clamp(xRotation - mouseY, -90f, 90f);
        }


        private void Look()
        {
            // cam.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
            Parent.transform.Rotate(Vector3.up, mouseX, Space.World);
            lookPivot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }
}