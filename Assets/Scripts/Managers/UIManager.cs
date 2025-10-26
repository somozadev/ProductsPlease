using TMPro;
using UnityEngine;
using ProductsPlease.Interactions;

namespace ProductsPlease.Managers
{
    public class UIManager : MonoBehaviour
    {
        public TMP_Text toolTip;
        public TMP_Text timeLeftDay;

        public TMP_Text allowedDestination1;
        public TMP_Text allowedDestination2;
        public TMP_Text allowedDestination3;

        public TMP_Text bannedDestination1;
        public TMP_Text bannedDestination2;
        public TMP_Text bannedDestination3;

        public TMP_Text declaredWeightRule;
        private string declaredStructure = "Declared weight must be <= 10.0kg";

        public TMP_Text realWeightRule;
        private string realStructure = "Real weight must be <= 12.0 kg";

        public TMP_Text DifferenceRule;
        private string diffStructure = "Difference must be \u2264 1.0 kg"; // ≤

        public TMP_Text priceRule;
        private string priceStructute = "Prices must be >= 500$";

        public TMP_Text radioactiveIsBannedText; // "are" / "are not"
        public TMP_Text metalsIsBannedText;      // "are" / "are not"
        public TMP_Text chemicalIsBannedText;    // "are" / "are not"

        public void PrintRecipe(){}
        public void LoadNewDayData()
        {
            var rules = GameManager.Instance.DaysManager.CurrentDayRules;
            if (rules == null)
            {
                ClearAll();
                return;
            }

            // --- time ---
            if (timeLeftDay)
                timeLeftDay.text = ToMMSS(rules.dayTimeSeconds);

            // --- destinations ---
            if (rules.destinationRule != null && rules.destinationRule.whitelistMode && rules.destinationRule.allowedDestinations != null)
            {
                // show whitelist, clear banned
                Fill3(allowedDestination1, allowedDestination2, allowedDestination3, rules.destinationRule.allowedDestinations);
                Fill3(bannedDestination1, bannedDestination2, bannedDestination3, null); // clears
            }
            else
            {
                // show banned list (can be empty), clear allowed
                var list = (rules.destinationRule != null) ? rules.destinationRule.forbiddenDestinations : null;
                Fill3(bannedDestination1, bannedDestination2, bannedDestination3, list);
                Fill3(allowedDestination1, allowedDestination2, allowedDestination3, null); // clears
            }

            // --- weight rules ---
            var wr = rules.weightRule ?? new WeightRule();

            if (declaredWeightRule)
            {
                if (wr.checkDeclared && wr.declaredComparator != Comparator.Any)
                {
                    // follow template: "Declared weight must be <= 10.0kg" (no space before kg)
                    declaredWeightRule.text = $"Declared weight must be {Cmp(wr.declaredComparator)} {wr.declaredKg:0.0}kg";
                }
                else declaredWeightRule.text = "";
            }

            if (realWeightRule)
            {
                if (wr.checkReal && wr.realComparator != Comparator.Any)
                {
                    // follow template: "Real weight must be <= 12.0 kg" (with space before kg)
                    realWeightRule.text = $"Real weight must be {Cmp(wr.realComparator)} {wr.realKg:0.0} kg";
                }
                else realWeightRule.text = "";
            }

            if (DifferenceRule)
            {
                if (wr.requireConsistency)
                {
                    // follow template: "Difference must be ≤ 1.0 kg"
                    DifferenceRule.text = $"Difference must be \u2264 {wr.maxAllowedDiffKg:0.0} kg";
                }
                else DifferenceRule.text = "";
            }

            // --- price rule ---
            var pr = rules.priceRule ?? new PriceRule();
            if (priceRule)
            {
                if (pr.enabled && pr.comparator != Comparator.Any)
                {
                    // follow template: "Prices must be >= 500$"
                    priceRule.text = $"Prices must be {Cmp(pr.comparator)} {pr.usd}$";
                }
                else priceRule.text = "";
            }

            // --- hidden attribute bans ---
            var attr = rules.attributeBanRule != null ? rules.attributeBanRule.forbiddenHiddenFlags : HiddenFlags.None;

            if (radioactiveIsBannedText)
                radioactiveIsBannedText.text = ((attr & HiddenFlags.IsRadioactive) != 0) ? "are" : "are not";
            if (metalsIsBannedText)
                metalsIsBannedText.text = ((attr & HiddenFlags.IsMetallic) != 0) ? "are" : "are not";
            if (chemicalIsBannedText)
                chemicalIsBannedText.text = ((attr & HiddenFlags.IsChemical) != 0) ? "are" : "are not";
        }

        // ----------------- helpers -----------------

        private void Fill3(TMP_Text t1, TMP_Text t2, TMP_Text t3, System.Collections.Generic.List<string> list)
        {
            string a = "", b = "", c = "";
            if (list != null)
            {
                if (list.Count > 0) a = list[0];
                if (list.Count > 1) b = list[1];
                if (list.Count > 2) c = list[2];
            }
            if (t1) t1.text = a;
            if (t2) t2.text = b;
            if (t3) t3.text = c;
        }

        private string Cmp(Comparator c)
        {
            switch (c)
            {
                case Comparator.LessEqual:    return "<=";
                case Comparator.GreaterEqual: return ">=";
                case Comparator.Equal:        return "==";
                case Comparator.NotEqual:     return "!=";
                default:                      return ""; // "Any" -> no símbolo
            }
        }

        private string ToMMSS(int seconds)
        {
            seconds = Mathf.Max(0, seconds);
            int m = seconds / 60;
            int s = seconds % 60;
            return $"{m:00}:{s:00}";
        }

        private void ClearAll()
        {
            if (timeLeftDay) timeLeftDay.text = "00:00";
            if (allowedDestination1) allowedDestination1.text = "";
            if (allowedDestination2) allowedDestination2.text = "";
            if (allowedDestination3) allowedDestination3.text = "";
            if (bannedDestination1) bannedDestination1.text = "";
            if (bannedDestination2) bannedDestination2.text = "";
            if (bannedDestination3) bannedDestination3.text = "";
            if (declaredWeightRule) declaredWeightRule.text = "";
            if (realWeightRule) realWeightRule.text = "";
            if (DifferenceRule) DifferenceRule.text = "";
            if (priceRule) priceRule.text = "";
            if (radioactiveIsBannedText) radioactiveIsBannedText.text = "are not";
            if (metalsIsBannedText) metalsIsBannedText.text = "are not";
            if (chemicalIsBannedText) chemicalIsBannedText.text = "are not";
        }
    }
}
