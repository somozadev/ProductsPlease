using ProductsPlease.Interactions;
using TMPro;
using UnityEngine;

namespace ProductsPlease.Tools
{
    [RequireComponent(typeof(Collider))]
    public class Scanner : MonoBehaviour
    {
        public enum ScanType
        {
            Info,
            Weight,
            Magnetic,
            Radiation,
            Chemical
        }

        [Header("Scan")] public ScanType scanType = ScanType.Info;

        [Tooltip("Seconds required to complete the scan.")]
        public float scanDuration = 1.25f;

        [Tooltip("If > 0, only counts time while item's |velocity| <= threshold.")]
        public float stableVelocityThreshold = 0.25f;

        [Tooltip("Automatically clear UI when the item leaves.")]
        public bool autoClearOnExit = true;

        [Header("Filtering")] [Tooltip("Layers considered as items on this pad.")]
        public LayerMask itemLayer = ~0;

        [Header("UI (TMP)")] public TMP_Text line1;
        public TMP_Text line2;
        public TMP_Text line3;
        public TMP_Text line4;
        public TMP_Text progressText; // optional

        // Runtime state
        Collider padCol;
        Item currentItem;
        Rigidbody currentRb;
        float dwellTimer;
        bool shown;

        void Awake()
        {
            padCol = GetComponent<Collider>();
            if (!padCol.isTrigger)
                Debug.LogWarning($"[ScannerPadSimple] Collider should be isTrigger = true on {name}");
            ClearUI();
        }

        void OnTriggerEnter(Collider other) => TryPickCandidate(other);

        void OnTriggerStay(Collider other)
        {
            if (!currentItem) TryPickCandidate(other);
        }

        void OnTriggerExit(Collider other)
        {
            var ic = other.GetComponentInParent<Item>();
            if (ic && ic == currentItem)
                ResetState(clearUi: autoClearOnExit);
        }

        void Update()
        {
            if (!currentItem || !currentRb || shown) return;

            bool stable = stableVelocityThreshold <= 0f ||
                          currentRb.linearVelocity.sqrMagnitude <= (stableVelocityThreshold * stableVelocityThreshold);

            if (stable)
            {
                dwellTimer += Time.deltaTime;
                SetProgress(dwellTimer / scanDuration);

                if (dwellTimer >= scanDuration)
                {
                    DumpResult(currentItem);
                    shown = true;
                }
            }
            else
            {
                // moved too much => reset timer but keep candidate
                dwellTimer = 0f;
                if (progressText) progressText.text = "HOLD STILL...";
            }
        }

        void TryPickCandidate(Collider other)
        {
            if (currentItem) return;
            if (((1 << other.gameObject.layer) & itemLayer.value) == 0) return;

            var ic = other.GetComponentInParent<Item>();
            var rb = other.attachedRigidbody;
            if (!ic || !rb) return;

            currentItem = ic;
            currentRb = rb;
            dwellTimer = 0f;
            shown = false;
            if (progressText) progressText.text = "SCANNING...";
        }

        void ResetState(bool clearUi)
        {
            currentItem = null;
            currentRb = null;
            dwellTimer = 0f;
            shown = false;
            if (clearUi) ClearUI();
        }

        void SetProgress(float t01)
        {
            if (!progressText) return;
            t01 = Mathf.Clamp01(t01);
            progressText.text = t01 < 1f ? $"SCANNING... {Mathf.RoundToInt(t01 * 100f)}%" : "DONE";
        }

        void ClearUI()
        {
            if (line1) line1.text = "";
            if (line2) line2.text = "";
            if (line3) line3.text = "";
            if (line4) line4.text = "";
            if (progressText) progressText.text = "";
        }

        void DumpResult(Item itemComp)
        {
            var d = itemComp.data;
            if (!d)
            {
                if (line1) line1.text = "NO DATA";
                if (progressText) progressText.text = "ERROR";
                return;
            }

            switch (scanType)
            {
                case ScanType.Info:
                {
                    bool fake = (d.hiddenFlags & HiddenFlags.FakeLabel) != 0;
                    if (line1) line1.text = $"Product: {d.displayName}";
                    if (line2) line2.text = $"Destination: {d.destination}";
                    if (line3) line3.text = $"Price: ${d.declaredPriceUSD}";
                    if (line4) line4.text = fake ? "<color=#FF6A6A>LABEL MISMATCH</color>" : "<color=#7CFF7C>LABEL OK</color>";
                    break;
                }
                case ScanType.Weight:
                {
                    float diff = Mathf.Abs(d.realWeightKg - d.declaredWeightKg);
                    if (line1) line1.text = $"Declared: {d.declaredWeightKg:0.0} kg";
                    if (line2) line2.text = $"Real:     {d.realWeightKg:0.0} kg";
                    if (line3) line3.text = $"Delta:    {diff:0.0} kg";
                    if (line4) line4.text = diff < 0.05f ? "<color=#7CFF7C>WITHIN TOLERANCE</color>" : "<color=#FFDE59>MISMATCH</color>";
                    break;
                }
                case ScanType.Magnetic:
                {
                    bool metal = (d.hiddenFlags & HiddenFlags.IsMetallic) != 0;
                    if (line1) line1.text = "Magnetic scan";
                    if (line2) line2.text = metal ? "<color=#6AC8FF>METAL DETECTED</color>" : "<color=#7CFF7C>CLEAR</color>";
                    if (line3) line3.text = $"Category: {d.productCategory}";
                    if (line4) line4.text = $"Item: {d.displayName}";
                    break;
                }
                case ScanType.Radiation:
                {
                    bool rad = (d.hiddenFlags & HiddenFlags.IsRadioactive) != 0;
                    if (line1) line1.text = "Radiation scan";
                    if (line2) line2.text = rad ? "<color=#FF6A6A>RADIOACTIVE</color>" : "<color=#7CFF7C>CLEAR</color>";
                    if (line3) line3.text = $"Category: {d.productCategory}";
                    if (line4) line4.text = $"Item: {d.displayName}";
                    break;
                }
                case ScanType.Chemical:
                {
                    bool chem = (d.hiddenFlags & HiddenFlags.IsChemical) != 0;
                    if (line1) line1.text = "Chemical scan";
                    if (line2) line2.text = chem ? "<color=#FFDE59>HAZARDOUS</color>" : "<color=#7CFF7C>CLEAR</color>";
                    if (line3) line3.text = $"Category: {d.productCategory}";
                    if (line4) line4.text = $"Item: {d.displayName}";
                    break;
                }
            }

            if (progressText) progressText.text = "DONE";
        }
    }
}