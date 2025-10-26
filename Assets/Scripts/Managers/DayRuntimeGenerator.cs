using UnityEngine;
using System;
using System.Collections.Generic;
using ProductsPlease.Interactions;

namespace ProductsPlease.Managers
{
    public class DayRuntimeGenerator : MonoBehaviour
    {
        [Header("Optional: seed for deterministic runs (-1 = random)")]
        public int seed = -1;

        // ---------- POOLS (edit for your game world) ----------
        static readonly string[] DESTINATIONS_POOL =
        {
            "Paris", "Madrid", "Rome", "Berlin", "Lisbon", "Vienna", "Prague",
            "Istanbul", "Dublin", "Warsaw", "Budapest", "Athens", "Copenhagen",
            "Stockholm", "Oslo", "Zurich", "Brussels", "Amsterdam", "London"
        };

        static readonly (ProductCategory cat, string[] names)[] CATEGORY_POOL =
        {
            (ProductCategory.Food, new[] { "Canned Goods", "Snack Box", "Dry Pasta", "Tea Assortment", "Coffee Beans" }),
            (ProductCategory.Medical, new[] { "Bandages", "Saline Kits", "Syringe Packs", "Gloves", "Masks" }),
            (ProductCategory.Electronics, new[] { "Electronic Parts", "PC Components", "Phone Chargers", "Batteries", "Sensors" }),
            (ProductCategory.Machinery, new[] { "Gear Set", "Spare Bolts", "Hydraulic Valves", "Bearings", "Shaft Kit" }),
            (ProductCategory.Chemicals, new[] { "Cleaning Solvent", "Lab Reagents", "Paint Thinner", "Adhesive Set", "Resin Kit" }),
            (ProductCategory.Documents, new[] { "Contracts", "Blueprints", "Forms", "Passports", "Certificates" }),
            (ProductCategory.Gifts, new[] { "Gift Box", "Souvenir Pack", "Toy Bundle", "Decor Set", "Board Game" }),
            (ProductCategory.Other, new[] { "Misc. Tools", "Household Items", "Craft Supplies", "Stationery", "Accessories" }),
        };

        // Probabilities (tune quickly for jam balance)
        [Header("Item generation probabilities")] [Range(0f, 1f)]
        public float pMetallic = 0.35f;

        [Range(0f, 1f)] public float pRadioactive = 0.05f;
        [Range(0f, 1f)] public float pChemical = 0.20f;
        [Range(0f, 1f)] public float pFakeLabel = 0.12f;

        [Header("Weight & price ranges")] public Vector2 declaredWeightRange = new Vector2(0.5f, 15f); // kg
        public Vector2 realWeightNoiseKg = new Vector2(-1.0f, 2.0f); // add to declared
        public Vector2Int priceRangeUSD = new Vector2Int(10, 800);

        System.Random sysRng;

        void Awake()
        {
            if (seed >= 0)
            {
                UnityEngine.Random.InitState(seed);
                sysRng = new System.Random(seed);
            }
            else
            {
                sysRng = new System.Random();
            }
        }

        // ======================================================
        //                 PUBLIC API (call these)
        // ======================================================

        /// <summary>
        /// Creates a runtime ItemData ScriptableObject with randomized, jam-friendly values.
        /// </summary>
        public ItemData GenerateNewRandomProduct()
        {
            var item = ScriptableObject.CreateInstance<ItemData>();

            // --- Destination & category/name ---
            string dest = DESTINATIONS_POOL[UnityEngine.Random.Range(0, DESTINATIONS_POOL.Length)];
            var catTuple = CATEGORY_POOL[UnityEngine.Random.Range(0, CATEGORY_POOL.Length)];
            var cat = catTuple.cat;
            string name = catTuple.names[UnityEngine.Random.Range(0, catTuple.names.Length)];

            // --- Declared weight & price ---
            float declared = UnityEngine.Random.Range(declaredWeightRange.x, declaredWeightRange.y);
            declared = (float)Math.Round(declared, 1);
            int price = UnityEngine.Random.Range(priceRangeUSD.x, priceRangeUSD.y + 1);

            // --- Real weight noise (biased) ---
            // 70%: exactly equal
            // 20%: tiny noise [-0.3..0.3]
            // 10%: anomaly [-2..+4]
            float noise = 0f;
            float r = UnityEngine.Random.value;
            if (r < 0.70f)
            {
                noise = 0f;
            }
            else if (r < 0.90f)
            {
                noise = UnityEngine.Random.Range(-0.3f, 0.3f);
            }
            else
            {
                noise = UnityEngine.Random.Range(-2.0f, 4.0f);
            }

            float real = Mathf.Max(0.1f, declared + noise);
            real = (float)Math.Round(real, 1);

            // --- Hidden flags (now actually assigned) ---
            HiddenFlags flags = HiddenFlags.None;

            // Metallic: base chance OR category-based
            if (Chance(pMetallic) || cat == ProductCategory.Electronics || cat == ProductCategory.Machinery)
                flags |= HiddenFlags.IsMetallic;

            // Radioactive: rare, and avoid for obviously benign
            if (Chance(pRadioactive) && cat != ProductCategory.Food && cat != ProductCategory.Gifts)
                flags |= HiddenFlags.IsRadioactive;

            // Chemical: base chance OR Chemicals category
            if (Chance(pChemical) || cat == ProductCategory.Chemicals)
                flags |= HiddenFlags.IsChemical;

            // Fake label
            if (Chance(pFakeLabel))
                flags |= HiddenFlags.FakeLabel;

            // --- Fill item fields ---
            item.itemId = Guid.NewGuid().ToString("N").Substring(0, 8);
            item.destination = dest;
            item.productCategory = cat;
            item.displayName = name;

            item.declaredWeightKg = declared;
            item.realWeightKg = real;
            item.declaredPriceUSD = price;

            item.tint = RandomPastel();

            item.hiddenFlags = flags;

            item.barcode = $"BC-{UnityEngine.Random.Range(100000, 999999)}";

            return item;
        }


        /// <summary>
        /// Creates a runtime DayParamsData ScriptableObject with randomized rules.
        /// Keeps things varied but readable for a jam session.
        /// </summary>
        public DayParamsData GenerateNewRandomDayRules()
        {
            var day = ScriptableObject.CreateInstance<DayParamsData>();

            // ---------- DESTINATION RULES ----------
            var destRule = new DestinationRule();

            // 30% chance to run whitelist-mode with 3 allowed destinations
            if (Chance(0.30f))
            {
                destRule.whitelistMode = true;
                int allowedCount = 3;
                destRule.allowedDestinations = PickDistinct(DESTINATIONS_POOL, allowedCount);
            }
            else
            {
                // Otherwise forbid 1–2 random destinations (20–50% chance)
                int forbidCount = Chance(0.50f) ? 2 : (Chance(0.20f) ? 1 : 0);
                if (forbidCount > 0)
                    destRule.forbiddenDestinations = PickDistinct(DESTINATIONS_POOL, forbidCount);
            }

            // 35% chance: some destinations require scan
            if (Chance(0.35f))
            {
                int reqCount = 2;
                destRule.requireScanForDestinations = PickDistinct(DESTINATIONS_POOL, reqCount);
            }

            day.destinationRule = destRule;

            // ---------- CATEGORY RULES ----------
            var catRule = new CategoryRule();
            if (Chance(0.25f))
            {
                // Whitelist 2 categories
                catRule.whitelistMode = true;
                catRule.allowed = PickDistinctEnum<ProductCategory>(2, skip: ProductCategory.Undefined);
            }
            else if (Chance(0.35f))
            {
                // Forbid 1 category
                catRule.forbidden = PickDistinctEnum<ProductCategory>(1, skip: ProductCategory.Undefined);
            }

            day.categoryRule = catRule;

            // ---------- WEIGHT RULES ----------
            var weightRule = new WeightRule();

            // 50%: check declared
            if (Chance(0.5f))
            {
                weightRule.checkDeclared = true;
                weightRule.declaredComparator = Chance(0.5f) ? Comparator.LessEqual : Comparator.GreaterEqual;
                weightRule.declaredKg = RandomRangeRound(4f, 12f, 0.5f);
            }

            // 70%: check real
            if (Chance(0.7f))
            {
                weightRule.checkReal = true;
                weightRule.realComparator = Chance(0.5f) ? Comparator.LessEqual : Comparator.GreaterEqual;
                weightRule.realKg = RandomRangeRound(4f, 12f, 0.5f);
            }

            // 45%: require consistency (max |real - declared| <= diff)
            if (weightRule.checkDeclared && weightRule.checkReal && Chance(0.45f))
            {
                weightRule.requireConsistency = true;
                weightRule.maxAllowedDiffKg = 1.0f; // simple default
            }

            day.weightRule = weightRule;

            // ---------- PRICE RULE ----------
            var priceRule = new PriceRule();
            if (Chance(0.45f))
            {
                priceRule.enabled = true;
                priceRule.comparator = Chance(0.65f) ? Comparator.LessEqual : Comparator.GreaterEqual;
                priceRule.usd = UnityEngine.Random.Range(150, 700);
            }

            day.priceRule = priceRule;

            // ---------- HIDDEN ATTRIBUTE BANS ----------
            var attrBan = new AttributeBanRule();
            // 40% forbid something hidden (1–2 flags)
            if (Chance(0.40f))
            {
                var hiddenChoices = new[] { HiddenFlags.IsMetallic, HiddenFlags.IsRadioactive, HiddenFlags.IsChemical };
                int count = Chance(0.5f) ? 1 : 2;
                for (int i = 0; i < count; i++)
                {
                    var pick = hiddenChoices[UnityEngine.Random.Range(0, hiddenChoices.Length)];
                    attrBan.forbiddenHiddenFlags |= pick;
                }
            }

            day.attributeBanRule = attrBan;

            // ---------- PROCESS RULES (forces station usage; optional for acceptance logic) ----------
            var proc = new ProcessRule
            {
                requireWeighAll = Chance(0.35f),
                requireScanAll = Chance(0.30f),
                requireMagneticCheck = Chance(0.25f),
                requireRadiationCheck = Chance(0.20f),
                requireChemicalCheck = Chance(0.25f),

                ifInternationalRequireScan = Chance(0.35f),
                ifElectronicsRequireMagnet = Chance(0.45f),
                ifMedicalRequireChemical = Chance(0.45f),
                ifRealHeavierThanDeclaredRequireRadiation = Chance(0.30f)
            };
            day.processRule = proc;

            // ---------- TUNING ----------
            day.maxBeltBuffer = 6;
            day.dayTimeSeconds = UnityEngine.Random.Range(75, 101);
            day.maxStationsPerItem = 0; // unlimited by default
            day.designerNotes = "Auto-generated day.";

            return day;
        }

        /// <summary>
        /// Validates an item against the day rules. Returns true if ACCEPTED.
        /// Fills violations with human-readable reasons for rejection.
        /// 
        /// Note: This focuses on logical acceptance (rules).
        /// Whether the player actually used required stations can be validated separately in gameplay.
        /// </summary>
        public bool EvaluateItemAgainstDay(ItemData item, DayParamsData day, out List<string> violations)
        {
            violations = new List<string>();

            // DESTINATION rules
            if (day.destinationRule != null)
            {
                var dr = day.destinationRule;
                if (dr.whitelistMode && dr.allowedDestinations.Count > 0)
                {
                    if (!dr.allowedDestinations.Contains(item.destination))
                        violations.Add($"Destination '{item.destination}' not in allowed whitelist.");
                }

                if (dr.forbiddenDestinations != null && dr.forbiddenDestinations.Contains(item.destination))
                {
                    violations.Add($"Destination '{item.destination}' is forbidden today.");
                }
            }

            // CATEGORY rules
            if (day.categoryRule != null)
            {
                var cr = day.categoryRule;
                if (cr.whitelistMode && cr.allowed.Count > 0)
                {
                    if (!cr.allowed.Contains(item.productCategory))
                        violations.Add($"Category '{item.productCategory}' not allowed (whitelist).");
                }

                if (cr.forbidden != null && cr.forbidden.Contains(item.productCategory))
                {
                    violations.Add($"Category '{item.productCategory}' is forbidden today.");
                }
            }

            // PRICE rule
            if (day.priceRule != null && day.priceRule.enabled)
            {
                if (!Compare(item.declaredPriceUSD, day.priceRule.comparator, day.priceRule.usd))
                {
                    violations.Add($"Price rule failed: {item.declaredPriceUSD} {Describe(day.priceRule.comparator)} {day.priceRule.usd}.");
                }
            }

            // WEIGHT rules
            if (day.weightRule != null)
            {
                var wr = day.weightRule;

                if (wr.checkDeclared)
                {
                    if (!Compare(item.declaredWeightKg, wr.declaredComparator, wr.declaredKg))
                        violations.Add($"Declared weight rule failed: {item.declaredWeightKg}kg {Describe(wr.declaredComparator)} {wr.declaredKg}kg.");
                }

                if (wr.checkReal)
                {
                    if (!Compare(item.realWeightKg, wr.realComparator, wr.realKg))
                        violations.Add($"Real weight rule failed: {item.realWeightKg}kg {Describe(wr.realComparator)} {wr.realKg}kg.");
                }

                if (wr.requireConsistency)
                {
                    float diff = Mathf.Abs(item.realWeightKg - item.declaredWeightKg);
                    if (diff > wr.maxAllowedDiffKg)
                        violations.Add($"Weight consistency failed: |real-declared|={diff:0.0}kg > {wr.maxAllowedDiffKg}kg.");
                }
            }

            // HIDDEN attribute bans
            if (day.attributeBanRule != null && day.attributeBanRule.forbiddenHiddenFlags != HiddenFlags.None)
            {
                var banned = day.attributeBanRule.forbiddenHiddenFlags;
                if ((item.hiddenFlags & banned) != 0)
                {
                    violations.Add($"Hidden attribute banned: {(item.hiddenFlags & banned)}.");
                }
            }

            // OPTIONAL: barcode vs label (FakeLabel) as an auto-reject rule
            // If you want FakeLabel to ALWAYS be a rejection (outside of attributeBanRule),
            // uncomment below:
            // if ((item.hiddenFlags & HiddenFlags.FakeLabel) != 0)
            //     violations.Add("Barcode mismatch (fake label).");

            return violations.Count == 0;
        }

        // ======================================================
        //                     HELPERS
        // ======================================================

        static bool Compare(int a, Comparator c, int b)
        {
            switch (c)
            {
                case Comparator.LessEqual: return a <= b;
                case Comparator.GreaterEqual: return a >= b;
                case Comparator.Equal: return a == b;
                case Comparator.NotEqual: return a != b;
                case Comparator.Any:
                default: return true;
            }
        }

        static bool Compare(float a, Comparator c, float b)
        {
            switch (c)
            {
                case Comparator.LessEqual: return a <= b + 1e-4f;
                case Comparator.GreaterEqual: return a >= b - 1e-4f;
                case Comparator.Equal: return Mathf.Abs(a - b) < 1e-3f;
                case Comparator.NotEqual: return Mathf.Abs(a - b) >= 1e-3f;
                case Comparator.Any:
                default: return true;
            }
        }

        static string Describe(Comparator c)
        {
            return c switch
            {
                Comparator.LessEqual => "<=",
                Comparator.GreaterEqual => ">=",
                Comparator.Equal => "==",
                Comparator.NotEqual => "!=",
                _ => "(any)"
            };
        }

        static bool Chance(float p) => UnityEngine.Random.value < Mathf.Clamp01(p);

        static float RandomRangeRound(float min, float max, float step)
        {
            float raw = UnityEngine.Random.Range(min, max);
            return Mathf.Round(raw / step) * step;
        }

        static List<string> PickDistinct(string[] source, int count)
        {
            var list = new List<string>(source);
            var result = new List<string>(count);
            for (int i = 0; i < count && list.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, list.Count);
                result.Add(list[idx]);
                list.RemoveAt(idx);
            }

            return result;
        }

        static List<ProductCategory> PickDistinctEnum<ProductCategory>(int count, ProductCategory skip)
        {
            var values = new List<ProductCategory>((ProductCategory[])System.Enum.GetValues(typeof(ProductCategory)));
            values.Remove(skip);
            var result = new List<ProductCategory>(count);
            for (int i = 0; i < count && values.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, values.Count);
                result.Add(values[idx]);
                values.RemoveAt(idx);
            }

            return result;
        }

        static Color RandomPastel()
        {
            // quick pastel color
            float h = UnityEngine.Random.value;
            float s = 0.45f;
            float v = 0.95f;
            Color c = Color.HSVToRGB(h, s, v);
            return c;
        }
    }
}