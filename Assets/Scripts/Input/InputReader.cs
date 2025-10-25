using System;
using UnityEngine;
using UnityEngine.InputSystem;

#pragma warning disable CS0414 // Field is assigned but its value is never used

namespace ProductsPlease.Player.Input
{
    [CreateAssetMenu(menuName = "Input")]
    public class InputReader : ScriptableObject, GameInputs.IDefaultActions, GameInputs.IUIActions
    {
        private GameInputs _gameInput;

        [SerializeField] private Vector2 playerMoveDebugger;
        [SerializeField] private bool interactDebugger;
        [SerializeField] private bool crouchDebugger;
        [SerializeField] private bool jumpDebugger;
        [SerializeField] private string currentSchemeDebugger;

        public event Action OnSchemeChanged;
        public string currentScheme { get; private set; }


        public event Action<Vector2> OnPlayerMoveEvent;
        public event Action<Vector2> OnLookEvent;
        public event Action<bool> OnInteractEvent;
        public event Action<bool> OnCrouchEvent;
        public event Action OnJumpEvent;


        private void OnEnable()
        {
            if (_gameInput != null) return;
            _gameInput = new GameInputs();
            _gameInput.Default.SetCallbacks(this);
            _gameInput.UI.SetCallbacks(this);
            SetGameplay();
        }

        private void OnDisable()
        {
            _gameInput.Default.Disable();
            _gameInput.UI.Disable();
        }

        #region ActionMapsSetters

        public void SetGameplay()
        {
            _gameInput.Default.Enable();
            _gameInput.UI.Disable();
        }

        public void SetUI()
        {
            _gameInput.UI.Enable();
            _gameInput.Default.Disable();
        }

        #endregion

        #region InputSchemeCallback

        private void DetectInputScheme(InputControl control)
        {
            var newScheme = control.device.name is "Keyboard" or "Mouse" ? "Keyboard&Mouse" : "Gamepad";
            if (newScheme == currentScheme) return;
            currentScheme = newScheme;
            OnSchemeChanged?.Invoke();
            currentSchemeDebugger = currentScheme;
        }

        #endregion

        #region ActionCallbacks

        public void OnMove(InputAction.CallbackContext context)
        {
            playerMoveDebugger = context.ReadValue<Vector2>();
            OnPlayerMoveEvent?.Invoke(playerMoveDebugger);
            DetectInputScheme(context.control);
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            OnLookEvent?.Invoke(context.ReadValue<Vector2>());
            DetectInputScheme(context.control);
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (context.performed || context.started)
                interactDebugger = true;
            else
                interactDebugger = false;
            OnInteractEvent?.Invoke(interactDebugger);
            DetectInputScheme(context.control);
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed || context.started)
            {
                jumpDebugger = true;
                OnJumpEvent?.Invoke();
            }
            else
                jumpDebugger = false;
            DetectInputScheme(context.control);
        }


        public void OnSprint(InputAction.CallbackContext context)
        {
            DetectInputScheme(context.control);
        }

        public void OnCrouch(InputAction.CallbackContext context)
        {
            if (context.performed || context.started)
                crouchDebugger = true;
            else
                crouchDebugger = false;
            OnCrouchEvent?.Invoke(crouchDebugger);
            DetectInputScheme(context.control);
        }

        public void OnRestart(InputAction.CallbackContext context)
        {
            throw new NotImplementedException();
        }

        public void OnNavigate(InputAction.CallbackContext context)
        {
            DetectInputScheme(context.control);
        }

        public void OnSubmit(InputAction.CallbackContext context)
        {
            DetectInputScheme(context.control);
        }

        public void OnCancel(InputAction.CallbackContext context)
        {
            DetectInputScheme(context.control);
        }

        public void OnPoint(InputAction.CallbackContext context)
        {
            DetectInputScheme(context.control);
        }

        public void OnClick(InputAction.CallbackContext context)
        {
            DetectInputScheme(context.control);
        }

        public void OnRightClick(InputAction.CallbackContext context)
        {
            DetectInputScheme(context.control);
        }

        public void OnMiddleClick(InputAction.CallbackContext context)
        {
            DetectInputScheme(context.control);
        }

        public void OnScrollWheel(InputAction.CallbackContext context)
        {
            DetectInputScheme(context.control);
        }

        public void OnTrackedDevicePosition(InputAction.CallbackContext context)
        {
            DetectInputScheme(context.control);
        }

        public void OnTrackedDeviceOrientation(InputAction.CallbackContext context)
        {
            DetectInputScheme(context.control);
        }

        #endregion

        #region SpecialMethods

        public Vector3 GetMousePosition()
        {
            return new Vector3(Mouse.current.position.ReadValue().x, Mouse.current.position.ReadValue().y, 0);
        }

        #endregion
    }
}