using ProductsPlease.Player;

namespace ProductsPlease
{
    using UnityEngine;

    public class CameraFollow : MonoBehaviour
    {
        public PlayerController player;

        private void FixedUpdate()
        {
            transform.position = player.CameraPivot.position;
        }
    }
}