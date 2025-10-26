using System;
using System.Collections;
using ProductsPlease.Interactions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            GameManager.Instance.dayCount++;

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

            int total = GameManager.Instance.GetComponent<BeltManager>().spawnedThisDay;

            int correctNotPlaced = total - GameManager.Instance.incorrectScansThisDay - GameManager.Instance.correctScansThisDay;
            GameManager.Instance.currentMoney += (correctNotPlaced * 10);
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

            // Actualiza la UI de tiempo después del fin de día
            UpdateTimeUI();
            
            // (opcional) aplica beneficios, si tienes otro sistema
            CheckEndGame();

        }

        [Header("End Game Fade")]
        [SerializeField] private CanvasGroup endFader;   // Panel negro con CanvasGroup (alpha 0 inicial)
        [SerializeField] private TMP_Text endLabel;      // Texto centrado (opcional)
        [SerializeField] private string endMessage = "BANKRUPT";
        [SerializeField] private float endFadeIn = 0.6f;
        [SerializeField] private float endHold   = 1.8f;
        [SerializeField] private float endFadeOut = 0.0f; // no necesitamos fade out si cambiamos de escena
        [SerializeField] private MonoBehaviour playerMotorToDisable; // opcional: tu componente de movimiento

        private bool isEnding;
     public void CheckEndGame()
        {
            if (isEnding) return;

            if (GameManager.Instance.currentMoney < 0)
            {
                StartCoroutine(CoEndGame());
            }
        }

        private IEnumerator CoEndGame()
        {
            isEnding = true;

            // Desactivar movimiento jugador (opcional)
            if (playerMotorToDisable) playerMotorToDisable.enabled = false;

            // Preparar UI
            if (endFader)
            {
                endFader.gameObject.SetActive(true);
                endFader.blocksRaycasts = true;
                endFader.interactable = false;
                endFader.alpha = 0f;
            }

            if (endLabel)
            {
                endLabel.gameObject.SetActive(true);
                endLabel.text = endMessage;
                var c = endLabel.color; c.a = 0f; endLabel.color = c;
            }

            // Fade IN a negro
            if (endFader)
            {
                float t = 0f;
                while (t < endFadeIn)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / Mathf.Max(0.01f, endFadeIn));
                    endFader.alpha = k;

                    // subir el alpha del label en el tramo final para que aparezca “sobre” el negro
                    if (endLabel)
                    {
                        var col = endLabel.color;
                        col.a = Mathf.Clamp01((k - 0.3f) / 0.7f); // aparece cuando el fondo ya está bastante negro
                        endLabel.color = col;
                    }
                    yield return null;
                }
                endFader.alpha = 1f;
                if (endLabel)
                {
                    var col = endLabel.color; col.a = 1f; endLabel.color = col;
                }
            }
            else
            {
                // Si no hay fader, simplemente espera
                yield return new WaitForSecondsRealtime(endFadeIn);
            }

            // Mantener mensaje un rato
            if (endHold > 0f) yield return new WaitForSecondsRealtime(endHold);

            // (Opcional) Fade out antes de cambiar (normalmente no hace falta)
            if (endFadeOut > 0f && endFader)
            {
                float t = 0f;
                while (t < endFadeOut)
                {
                    t += Time.unscaledDeltaTime;
                    float k = 1f - Mathf.Clamp01(t / Mathf.Max(0.01f, endFadeOut));
                    endFader.alpha = k;
                    yield return null;
                }
            }

            // Cargar escena 0
            SceneManager.LoadScene(0);
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