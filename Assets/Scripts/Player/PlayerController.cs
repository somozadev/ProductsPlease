using System;
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
        private PlayerInteraction playerInteraction { get; set; }
        
        
        public Transform CameraPivot;


        private void Awake()
        {
            Camera = Camera.main;

            CharacterController = GetComponent<CharacterController>();
            playerMotor = GetComponentInChildren<PlayerMotor>();
            playerInteraction = GetComponentInChildren<PlayerInteraction>();
            playerLook = GetComponentInChildren<PlayerLook>();

            playerMotor.Initialise();
            playerInteraction.Initialise();
            playerLook.Initialise();
        }

        private void Start()
        {
            AudioManager.Instance.PlaySFX("Intro");

        }

        private void OnEnable()
        {
            playerMotor.OnEnabled();
            playerInteraction.OnEnabled();
            playerLook.OnEnabled();
        }

        private void OnDisable()
        {
            playerMotor.OnDisabled();
            playerInteraction.OnDisabled();
            playerLook.OnDisabled();
        }
    }
}