using ProductsPlease.Interactions;
using TMPro;
using UnityEngine;

namespace ProductsPlease.Tools
{
    [RequireComponent(typeof(Collider))]
    public class Sensor : MonoBehaviour
    {
        public enum ScanType
        {
            Info,
            Weight,
            Magnetic,
            Radiation,
            Chemical
        }

        [Header("Setup")]
        public ScanType scanType = ScanType.Info;
        public LayerMask itemLayer;

        [Header("UI")]
        
        public TextMeshProUGUI line1;
        public TextMeshProUGUI line2;
        public TextMeshProUGUI line3;
        public TextMeshProUGUI line4;
        public TextMeshProUGUI progressText;

        [Header("Logic")]
        public float scanDuration = 0.4f;
        public float stableVelocityThreshold = 0.05f;
        public bool autoClearOnExit = true;

        Collider padCol;
        Item currentItem;
        Rigidbody currentRb;
        float dwellTimer;
        bool shown;

        void Awake()
        {
            padCol = GetComponent<Collider>();
            if (!padCol.isTrigger)
                Debug.LogWarning($"[Sensor] Collider should be isTrigger = true on {name}");
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
            if (scanType != ScanType.Info && scanType != ScanType.Weight)
            {
                DumpResult(currentItem.data);
                return;
            }
            // Hold still while on pad
            var speed = currentRb.linearVelocity.magnitude;
            if (speed < stableVelocityThreshold)
            {
                dwellTimer += Time.deltaTime;
                if (progressText) progressText.text = $"{Mathf.Clamp01(dwellTimer / scanDuration) * 100f:0}%";
                if (dwellTimer >= scanDuration)
                {
                    DumpResult(currentItem.data);
                    shown = true;
                }
            }
            else
            {
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

        void ClearUI()
        {
            if (line1) line1.text = string.Empty;
            if (line2) line2.text = string.Empty;
            if (line3) line3.text = string.Empty;
            if (line4) line4.text = string.Empty;
            if (progressText) progressText.text = string.Empty;
        }

        void DumpResult(ItemData d)
        {
            if (!d)
            {
                ClearUI();
                return;
            }

            switch (scanType)
            {
                case ScanType.Info:
                {
                    // Compare visible label against official verification data
                    var v = d.verification;
                    bool destMismatch   = !string.Equals(d.destination, v.officialDestination, System.StringComparison.Ordinal);
                    bool catMismatch    = d.productCategory != v.officialCategory;
                    bool weightMismatch = Mathf.Abs(d.declaredWeightKg - v.officialDeclaredWeightKg) > 0.05f;
                    bool priceMismatch  = d.declaredPriceUSD != v.officialDeclaredPriceUSD;

                    bool anyMismatch = destMismatch || catMismatch || weightMismatch || priceMismatch;

                    if (line1) line1.text = $"Product: {d.displayName}";
                    if (line2) line2.text = $"Destination: {d.destination}";
                    if (line3) line3.text = $"Price: ${d.declaredPriceUSD} | Decl. W: {d.declaredWeightKg:0.0}kg";

                    if (!anyMismatch && (d.hiddenFlags & HiddenFlags.FakeLabel) == 0)
                    {
                        if (line4) line4.text = $"<color=#7CFF7C>LABEL OK</color> <size=70%>(sig {v.signature})</size>";
                    }
                    else
                    {
                        if (line4) line4.text = "<color=#FF6A6A>LABEL MISMATCH</color>";
                        // Show reasons on lines 2-3-4 compactly if needed
                        string reasons = string.Empty;
                        if (destMismatch)   reasons += $"Dest: '{d.destination}' vs '{v.officialDestination}'\n";
                        if (catMismatch)    reasons += $"Cat: {d.productCategory} vs {v.officialCategory}\n";
                        if (weightMismatch) reasons += $"Decl W: {d.declaredWeightKg:0.0} vs {v.officialDeclaredWeightKg:0.0}\n";
                        if (priceMismatch)  reasons += $"Price: ${d.declaredPriceUSD} vs ${v.officialDeclaredPriceUSD}\n";

                        // Reuse lower lines for reasons
                        if (line2) line2.text = reasons.TrimEnd();
                        if (line3) line3.text = $"sig {v.signature}";
                    }
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
                    // if (line1) line1.text = "Magnetic scan";
                    if (line1) line1.text = metal ? "<color=#6AC8FF>METAL DETECTED</color>" : "<color=#7CFF7C>CLEAR</color>";
                    // if (line3) line3.text = $"Category: {d.productCategory}";
                    // if (line4) line4.text = $"Item: {d.displayName}";
                    break;
                }
                case ScanType.Radiation:
                {
                    //Emit bip
                    //show red light
                    
                    bool rad = (d.hiddenFlags & HiddenFlags.IsRadioactive) != 0;
                    if (line1) line1.text = rad ? "<color=#FF6A6A>RADIOACTIVE</color>" : "<color=#7CFF7C>CLEAR</color>";
                    // if (line3) line3.text = $"Category: {d.productCategory}";
                    // if (line4) line4.text = $"Item: {d.displayName}";
                    break;
                }
                case ScanType.Chemical:
                {
                    bool chem = (d.hiddenFlags & HiddenFlags.IsChemical) != 0;
                    if (line1) line1.text = chem ? "<color=#FFDE59>HAZARDOUS</color>" : "<color=#7CFF7C>CLEAR</color>";
                    // if (line3) line3.text = $"Category: {d.productCategory}";
                    // if (line4) line4.text = $"Item: {d.displayName}";
                    break;
                }
            }

            if (progressText) progressText.text = "DONE";
        }
    }
}
