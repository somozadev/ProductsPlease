using UnityEngine;

namespace ProductsPlease.Player
{
    public class PlayerLook : PlayerComponent
    {
        [SerializeField] private float xSensitivity = 3.5f;
        [SerializeField] private float ySensitivity = 3.5f;
        [SerializeField] private Transform lookPivot;

        [SerializeField] private float initialYawOffsetY = 0f;   
        [SerializeField] private float initialPitchOffsetX = 0f; 

        private float yawDelta;   
        private float pitchDelta; 

        private float baseYaw;    
        private float basePitch;  

        private Camera cam;
        private Input.InputReader inputReader;

        public override void Initialise()
        {
            base.Initialise();
            cam = Parent.Camera;
            inputReader = Parent.inputReader;
            if (!lookPivot) lookPivot = transform;

            baseYaw   = NormalizeAngle(Parent.transform.localEulerAngles.y) + initialYawOffsetY;
            basePitch = NormalizeAngle(lookPivot.localEulerAngles.x)       + initialPitchOffsetX;

            yawDelta = 0f;
            pitchDelta = 0f;

            ApplyLook();
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
            ApplyLook();
        }

        private void ProcessLook(Vector2 input)
        {
            yawDelta   += input.x * xSensitivity;
            pitchDelta -= input.y * ySensitivity;

            pitchDelta = Mathf.Clamp(pitchDelta, -90f, 90f);
        }

        private void ApplyLook()
        {
            float yaw   = baseYaw   + yawDelta;
            float pitch = Mathf.Clamp(basePitch + pitchDelta, -89.9f, 89.9f);

            Parent.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            lookPivot.localRotation        = Quaternion.Euler(pitch, 0f, 0f);
        }

        private static float NormalizeAngle(float a)
        {
            a %= 360f;
            if (a > 180f) a -= 360f;
            return a;
        }
    }
}
