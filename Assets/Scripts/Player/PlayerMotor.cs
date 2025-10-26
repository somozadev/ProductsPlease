using System;
using UnityEngine;

namespace ProductsPlease.Player
{
    public class PlayerMotor : PlayerComponent
    {
        private const float speed = 5f;
        private const float gravity = -20f;
        private const float jumpHeight = 2f;
        private Input.InputReader inputReader;
        private Vector3 moveDirection;
        private Vector3 moveVelocity;
        private bool isGrounded;
        private bool isCrouched;

        private float crouchedHeight = .75f;
        private float standingHeight = 1.75f;
        private const float standingCenterY = 1f;
        private const float crouchedCenterY = 0.3525f;
        private const float crouchLerpSpeed = 10f;

        [SerializeField] private Vector3 standingCamPos;
        [SerializeField] private Vector3 crouchedCamPos;
        private Camera cam;


        public override void Initialise()
        {
            base.Initialise();
            cam = Parent.Camera;
            inputReader = Parent.inputReader;
        }

        public override void OnEnabled()
        {
            base.OnEnabled();
            inputReader.OnPlayerMoveEvent += ProcessMove;
            inputReader.OnJumpEvent += Jump;
            inputReader.OnCrouchEvent += Crouch;
        }

        public override void OnDisabled()
        {
            base.OnDisabled();
            inputReader.OnPlayerMoveEvent -= ProcessMove;
            inputReader.OnJumpEvent -= Jump;
            inputReader.OnCrouchEvent -= Crouch;
        }

   

        private void Update()
        {
            Move();
            Gravity();
            isGrounded = Parent.CharacterController.isGrounded;

            float targetHeight = isCrouched ? crouchedHeight : standingHeight;
            float targetCenter = isCrouched ? crouchedCenterY : standingCenterY;

            Parent.CharacterController.height = Mathf.Lerp(Parent.CharacterController.height, targetHeight, Time.deltaTime * crouchLerpSpeed);
            cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition, isCrouched ? crouchedCamPos : standingCamPos, Time.deltaTime * crouchLerpSpeed);

            Vector3 center = Parent.CharacterController.center;
            center.y = Mathf.Lerp(center.y, targetCenter, Time.deltaTime * crouchLerpSpeed);
            Parent.CharacterController.center = center;
        }

        private void ProcessMove(Vector2 input)
        {
            moveDirection = Vector3.zero;
            moveDirection.x = input.x;
            moveDirection.z = input.y;
        }

        private void Jump()
        {
            if (!isGrounded) return;
            moveVelocity.y = MathF.Sqrt(jumpHeight * -gravity);
        }

        private void Crouch(bool value)
        {
            // if (!isGrounded) return;
            isCrouched = value;
        }

        private void Move()
        {
            if (moveDirection == Vector3.zero)
                return;
            Parent.CharacterController.Move(transform.TransformDirection(moveDirection) * (speed * Time.deltaTime));
        }

        private void Gravity()
        {
            if (isGrounded && moveVelocity.y < 0)
                moveVelocity.y = -2;
            else
                moveVelocity.y += gravity * Time.deltaTime;
            Parent.CharacterController.Move(moveVelocity * Time.deltaTime);
        }
    }
}