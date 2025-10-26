using System;
using ProductsPlease.Interactions;
using UnityEngine;

namespace ProductsPlease.Managers
{
    public class DaysManager : MonoBehaviour
    {
        [Header("Day Time")] [SerializeField] private float currentTime;
        [SerializeField] private float maxDayTime = 60f * 3f; // tiempo base del día en segundos
        public float timeMultiplier = 1.0f;

        [Header("Time Bonus (for next day)")] [Tooltip("Seconds added to next day's max time per (correct - incorrect). Clamped >= 0.")]
        public float timeBonusPerNetCorrect = 3f;

        [Tooltip("Maximum seconds that can be added in a single day as bonus.")]
        public float maxBonusPerDay = 60f;

        public int dayCount { get; private set; }
        public bool dayInProgress { get; private set; }

        public BeltManager belt;
        public DayRuntimeGenerator generator;

        [SerializeField] DayParamsData currentDayRules;
        public DayParamsData CurrentDayRules => currentDayRules;

        // (opcional) info del último día para UI/estadísticas
        public int lastDayCorrect { get; private set; }
        public int lastDayIncorrect { get; private set; }
        public float lastDayTimeBonus { get; private set; }

        private void Start()
        {
            currentDayRules = generator.GenerateNewRandomDayRules();
            belt.StartBelt();
        }

        private void Update()
        {
            if (!dayInProgress) return;

            currentTime -= Time.deltaTime * timeMultiplier;
            if (currentTime <= 0f)
            {
                FinishDay();
            }

            UpdateTimeUI();
        }

        [ContextMenu("StartNewDay")]
        public void StartNewDay()
        {
            dayCount++;
            currentTime = maxDayTime;
            dayInProgress = true;
            GameManager.Instance.GetComponent<BeltManager>().dayStarted = true;

            // reinicia la cinta con las reglas ya preparadas para este día
            belt.BeginDay(currentDayRules);
        }

        public void FinishDay()
        {
            if (!dayInProgress) return;

            dayInProgress = false;
            GameManager.Instance.GetComponent<BeltManager>().dayStarted = false;

            // ----- 1) Leer puntuaciones del GameManager -----
            int correct = GameManager.Instance?.correctScansThisDay ?? 0;
            int incorrect = GameManager.Instance?.incorrectScansThisDay ?? 0;
            lastDayCorrect = correct;
            lastDayIncorrect = incorrect;

            // ----- 2) Calcular bonus de tiempo para el PRÓXIMO día -----
            int net = Mathf.Max(0, correct - incorrect);
            float bonus = Mathf.Clamp(net * timeBonusPerNetCorrect, 0f, maxBonusPerDay);
            lastDayTimeBonus = bonus;

            // El próximo día tendrá más tiempo base
            maxDayTime += bonus;

            // (opcional) resetear contadores para el próximo día
            GameManager.Instance.correctScansThisDay = 0;
            GameManager.Instance.incorrectScansThisDay = 0;

            // ----- 3) Llamar a la UI para imprimir el “recap/recipe” -----
            GameManager.Instance.UIManager?.PrintRecipe();

            // ----- 4) Preparar reglas nuevas para el siguiente día -----
            currentDayRules = generator.GenerateNewRandomDayRules();

            // (opcional) parar la cinta aquí si procede
            // belt.StopBelt();

            // resetea el tiempo actual a la base (se aplicará cuando llames StartNewDay)
            currentTime = maxDayTime;

            // (opcional) aplica beneficios, si tienes otro sistema
            CheckBenefits();

            // Actualiza la UI de tiempo después del fin de día
            UpdateTimeUI();
        }

        public void CheckBenefits()
        {
            // Hook para recompensas/desbloqueos si lo necesitas
        }

        private void UpdateTimeUI()
        {
            if (GameManager.Instance.UIManager == null) return;

            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, currentTime));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            GameManager.Instance.UIManager.timeLeftDay.text = $"{minutes:00}:{seconds:00}";
        }
    }
}