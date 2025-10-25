using UnityEngine;
namespace ProductsPlease.Player
{
    
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] public Input.InputReader inputReader;
        public Camera Camera { get; private set; }
        public CharacterController CharacterController { get; private set; }
        private PlayerMotor playerMotor { get; set; }
        private PlayerLook playerLook { get; set; }

        
        private void Awake()
        {
            Camera = Camera.main;
            
            CharacterController = GetComponent<CharacterController>();
            playerMotor = GetComponentInChildren<PlayerMotor>();
            playerLook = GetComponentInChildren<PlayerLook>();

            playerMotor.Initialise();
            playerLook.Initialise();
            OnEnable();
        }
        private void OnEnable()
        {
            playerMotor.OnEnabled();
            playerLook.OnEnabled();
        }

        private void OnDisable()
        {
            playerMotor.OnDisabled();
            playerLook.OnDisabled();
        }
    }
}