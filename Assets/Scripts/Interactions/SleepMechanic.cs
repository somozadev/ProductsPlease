using System.Collections;
using TMPro;
using UnityEngine;

namespace Interactions
{
    public class SleepMechanic : MonoBehaviour
    {
        [Header("UI")]
        [Tooltip("CanvasGroup de un panel negro a pantalla completa (alpha 0 al inicio).")]
        public CanvasGroup blackFade;
        [Tooltip("Texto para mostrar 'Day X'. Puede estar vacío inicialmente.")]
        public TMP_Text dayLabel;

        [Header("Timings")]
        public float fadeInDuration  = 0.6f;
        public float holdDuration    = 3.0f;  // tiempo mostrando "Day X"
        public float fadeOutDuration = 0.6f;
        public bool useUnscaledTime  = true;

        [Header("Player")]
        [Tooltip("Componente de movimiento del jugador (se desactiva/activa al dormir).")]
        public MonoBehaviour playerMotor;

        [Header("Cursor (opcional)")]
        public bool hideCursorDuringSleep = true;

        private bool isSleeping;

        /// <summary>
        /// Lanza la secuencia de "sleep" mostrando 'Day X'.
        /// </summary>
        public void StartSleep(int dayNumber)
        {
            if (!isSleeping && gameObject.activeInHierarchy)
                StartCoroutine(CoSleep(dayNumber));
        }

        /// <summary>
        /// Atajo de prueba en editor (click derecho en el componente).
        /// </summary>
        [ContextMenu("Test Sleep (Day 1)")]
        private void TestSleep() => StartSleep(1);

        private IEnumerator CoSleep(int dayNumber)
        {
            isSleeping = true;

            if (blackFade == null)
            {
                Debug.LogError("[SleepMechanic] Missing CanvasGroup (blackFade).");
                isSleeping = false;
                yield break;
            }

            // Asegura estado inicial
            blackFade.gameObject.SetActive(true);
            SetAlpha(blackFade, 0f);

            if (dayLabel)
            {
                dayLabel.gameObject.SetActive(true);
                dayLabel.text = $"Day {dayNumber}";
                // ocultamos el texto hasta estar en negro (opcional: alpha del color)
                var c = dayLabel.color; c.a = 0f; dayLabel.color = c;
            }

            // Cursor off (opcional)
            if (hideCursorDuringSleep)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }

            // DESACTIVAR movimiento al empezar fade a negro
            if (playerMotor) playerMotor.enabled = false;

            // ---- Fade IN a negro ----
            yield return FadeTo(blackFade, 1f, fadeInDuration);

            // Mostrar el "Day X" una vez la pantalla está negra
            if (dayLabel)
            {
                // subimos alpha del texto para que se vea sobre el negro
                float t = 0f;
                float dur = 0.2f;
                while (t < dur)
                {
                    t += Delta();
                    var c = dayLabel.color;
                    c.a = Mathf.Clamp01(t / dur);
                    dayLabel.color = c;
                    yield return null;
                }
            }

            // ---- Espera/hold ----
            float hold = Mathf.Max(0f, holdDuration);
            float elapsed = 0f;
            while (elapsed < hold)
            {
                elapsed += Delta();
                yield return null;
            }

            // Al empezar el fade back, REACTIVAR movimiento
            if (playerMotor) playerMotor.enabled = true;

            // Oculta suavemente el label (opcional)
            if (dayLabel)
            {
                float t = 0f;
                float dur = 0.15f;
                while (t < dur)
                {
                    t += Delta();
                    var c = dayLabel.color;
                    c.a = 1f - Mathf.Clamp01(t / dur);
                    dayLabel.color = c;
                    yield return null;
                }
            }

            // ---- Fade OUT (volver a transparente) ----
            yield return FadeTo(blackFade, 0f, fadeOutDuration);

            // Limpieza
            if (dayLabel) dayLabel.gameObject.SetActive(false);
            blackFade.gameObject.SetActive(false);

            // (Opcional) restaurar cursor aquí si lo usas en gameplay
            // Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;

            isSleeping = false;
        }

        private IEnumerator FadeTo(CanvasGroup cg, float target, float duration)
        {
            duration = Mathf.Max(0.01f, duration);
            float start = cg.alpha;
            float t = 0f;

            while (t < duration)
            {
                t += Delta();
                float k = Mathf.Clamp01(t / duration);
                cg.alpha = Mathf.Lerp(start, target, k);
                yield return null;
            }
            cg.alpha = target;
        }

        private void SetAlpha(CanvasGroup cg, float a) => cg.alpha = Mathf.Clamp01(a);

        private float Delta() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }
}
