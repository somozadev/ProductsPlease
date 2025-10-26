using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using ProductsPlease.Interactions;
using ProductsPlease.Managers;

namespace ProductsPlease.World
{
    [RequireComponent(typeof(Collider))]
    public class BeltEnd : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private LayerMask itemLayer = ~0;   
        [SerializeField] private bool consumeItem = true;    

        [Header("Refs")]
        [SerializeField] private DayRuntimeGenerator generator; 

        [Header("Events")]
        public UnityEvent onAccepted;
        public UnityEvent onRejected;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
            generator = FindAnyObjectByType<DayRuntimeGenerator>(FindObjectsInactive.Include);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & itemLayer.value) == 0)
                return;

            var item = other.GetComponentInParent<Item>();
            if (!item || !item.data) return;

            var rules = GameManager.Instance?.DaysManager?.CurrentDayRules;
            if (rules == null)
            {
                Debug.LogWarning("[BeltEnd] No DayRules found in GameManager.");
                return;
            }

            if (!generator)
            {
                generator = FindAnyObjectByType<DayRuntimeGenerator>(FindObjectsInactive.Include);
                if (!generator)
                {
                    Debug.LogError("[BeltEnd] DayRuntimeGenerator not found.");
                    return;
                }
            }

            if (generator.EvaluateItemAgainstDay(item.data, rules, out List<string> violations))
            {
                GameManager.Instance.correctScansThisDay++;
                GameManager.Instance.currentMoney += 10;
                onAccepted?.Invoke();
            }
            else
            {
                GameManager.Instance.incorrectScansThisDay++;
                GameManager.Instance.currentMoney -= 10;
                if (violations != null && violations.Count > 0)
                    Debug.Log($"[BeltEnd] Rejected '{item.data.displayName}': {string.Join("; ", violations)}");
                onRejected?.Invoke();
            }

            if (consumeItem)
            {
                Destroy(item.gameObject);
            }
        }
    }
}
