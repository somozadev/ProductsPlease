using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ProductsPlease.Player
{
    public class PlayerLook : PlayerComponent
    {
        [SerializeField] private float xSensitivity = 3.5f;
        [SerializeField] private float ySensitivity = 3.5f;
        [SerializeField] private Transform lookPivot;
        private float mouseX = 0f;
        private float mouseY = 0f;

        float yaw;  
        float pitch; 
        
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

            yaw   += mouseX;
            pitch  = Mathf.Clamp(pitch - mouseY, -90f, 90f);
            
        }

        private void Look()
        {
            Parent.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            lookPivot.localRotation        = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}