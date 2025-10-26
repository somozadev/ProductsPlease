using UnityEngine;

namespace ProductsPlease.Interactions
{
    public class PlayStopAnim : MonoBehaviour
    {
        public Animator Animator;
        private bool isPaused = false;

        public void PlayStopToggle()
        {
            if (!Animator)
                return;
            isPaused = !isPaused;
            Animator.speed = isPaused ? 0f : 1f;
        }
    }
}