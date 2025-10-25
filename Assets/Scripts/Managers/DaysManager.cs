using System;
using UnityEngine;

namespace ProductsPlease.Managers
{
    public class DaysManager : MonoBehaviour
    {
        [SerializeField] private float currentTime;
        [SerializeField] private float maxDayTime = 60f * 3f;

        public int dayCount { get; private set; }
        public bool dayInProgress { get; private set; }
        public float timeMultiplier = 1.0f;

        private void Update()
        {
            if (!dayInProgress) return;
            currentTime -= Time.deltaTime * timeMultiplier;
            if (currentTime <= 0)
                FinishDay();

            UpdateTimeUI();
        }

        [ContextMenu("StartNewDay")]
        public void StartNewDay()
        {
            dayCount++;
            currentTime = maxDayTime;
            dayInProgress = true;
        }

        public void FinishDay()
        {
            dayInProgress = false;
            currentTime = maxDayTime;
            CheckBenefits();
        }

        public void CheckBenefits()
        {
        }

        private void UpdateTimeUI()
        {
            if (GameManager.Instance.UIManager == null) return;

            int totalSeconds = Mathf.CeilToInt(currentTime);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            GameManager.Instance.UIManager.timeLeftDay.text = $"{minutes:00}:{seconds:00}";
        }
    }
}