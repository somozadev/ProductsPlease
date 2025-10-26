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
            "Paris","Madrid","Rome","Berlin","Lisbon","Vienna","Prague",
            "Istanbul","Dublin","Warsaw","Budapest","Athens","Copenhagen",
            "Stockholm","Oslo","Zurich","Brussels","Amsterdam","London"
        };

        static readonly (ProductCategory cat, string[] names)[] CATEGORY_POOL =
        {
            (ProductCategory.Food,        new[]{ "Canned Goods","Snack Box","Dry Pasta","Tea Assortment","Coffee Beans" }),
            (ProductCategory.Medical,     new[]{ "Bandages","Saline Kits","Syringe Packs","Gloves","Masks" }),
            (ProductCategory.Electronics, new[]{ "Electronic Parts","PC Components","Phone Chargers","Batteries","Sensors" }),
            (ProductCategory.Machinery,   new[]{ "Gear Set","Spare Bolts","Hydraulic Valves","Bearings","Shaft Kit" }),
            (ProductCategory.Chemicals,   new[]{ "Cleaning Solvent","Lab Reagents","Paint Thinner","Adhesive Set","Resin Kit" }),
            (ProductCategory.Documents,   new[]{ "Contracts","Blueprints","Forms","Passports","Certificates" }),
            (ProductCategory.Gifts,       new[]{ "Gift Box","Souvenir Pack","Toy Bundle","Decor Set","Board Game" }),
            (ProductCategory.Other,       new[]{ "Misc. Tools","Household Items","Craft Supplies","Stationery","Accessories" }),
        };

        [Header("Item generation probabilities")]
        [Range(0f,1f)] public float pMetallic     = 0.35f;
        [Range(0f,1f)] public float pRadioactive  = 0.05f;
        [Range(0f,1f)] public float pChemical     = 0.20f;
        [Range(0f,1f)] public float pFakeLabel    = 0.12f;

        [Header("Weight & price ranges")]
        public Vector2 declaredWeightRange = new Vector2(0.5f, 15f); // kg
        public Vector2 realWeightNoiseKg   = new Vector2(-1.0f, 2.0f); // add to declared
        public Vector2Int priceRangeUSD    = new Vector2Int(10, 800);

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
        //                 PUBLIC API
        // ======================================================

        /// <summary>Create a randomized ItemData ScriptableObject.</summary>
        public ItemData GenerateNewRandomProduct()
        {
            var item = ScriptableObject.CreateInstance<ItemData>();

            // Destination & category
            string dest = DESTINATIONS_POOL[UnityEngine.Random.Range(0, DESTINATIONS_POOL.Length)];
            var catTuple = CATEGORY_POOL[UnityEngine.Random.Range(0, CATEGORY_POOL.Length)];
            var cat = catTuple.cat;
            string name = catTuple.names[UnityEngine.Random.Range(0, catTuple.names.Length)];

            // Declared weight & price
            float declared = UnityEngine.Random.Range(declaredWeightRange.x, declaredWeightRange.y);
            int price = UnityEngine.Random.Range(priceRangeUSD.x, priceRangeUSD.y + 1);

            // Real weight
            float noise = UnityEngine.Random.Range(realWeightNoiseKg.x, realWeightNoiseKg.y);
            float real = Mathf.Max(0.1f, declared + noise);

            // Hidden flags
            HiddenFlags flags = HiddenFlags.None;
            if (Chance(pMetallic) || cat == ProductCategory.Electronics || cat == ProductCategory.Machinery)
                flags |= HiddenFlags.IsMetallic;
            if (Chance(pRadioactive) && cat != ProductCategory.Food && cat != ProductCategory.Gifts)
                flags |= HiddenFlags.IsRadioactive;
            if (Chance(pChemical) || cat == ProductCategory.Chemicals)
                flags |= HiddenFlags.IsChemical;
            if (Chance(pFakeLabel))
                flags |= HiddenFlags.FakeLabel;

            // Fill item
            item.itemId = Guid.NewGuid().ToString("N").Substring(0, 8);
            item.destination = dest;
            item.productCategory = cat;
            item.declaredWeightKg = (float)Math.Round(declared, 1);
            item.declaredPriceUSD = price;
            item.realWeightKg = (float)Math.Round(real, 1);
            item.displayName = name;
            item.tint = RandomPastel();
            item.barcode = $"BC-{UnityEngine.Random.Range(100000, 999999)}";
            item.hiddenFlags = flags;

            // Setup verification data (official)
            item.verification.officialDestination = item.destination;
            item.verification.officialCategory = item.productCategory;
            item.verification.officialDeclaredWeightKg = item.declaredWeightKg;
            item.verification.officialDeclaredPriceUSD = item.declaredPriceUSD;
            item.verification.signature = ComputeSignature(
                item.verification.officialDestination,
                item.verification.officialCategory,
                item.verification.officialDeclaredWeightKg,
                item.verification.officialDeclaredPriceUSD
            );

            // If FakeLabel, alter at least one official field to create a mismatch
            if ((flags & HiddenFlags.FakeLabel) != 0)
            {
                // Change official destination to a different one
                item.verification.officialDestination = PickDifferentDestination(item.destination);
                // Maybe also change other fields for variety
                if (Chance(0.5f))
                    item.verification.officialCategory = RandomDifferentCategory(item.productCategory);
                if (Chance(0.4f))
                {
                    float tweak = UnityEngine.Random.Range(-1.0f, 1.0f);
                    item.verification.officialDeclaredWeightKg = Mathf.Max(0.1f, item.declaredWeightKg + tweak);
                }
                if (Chance(0.4f))
                {
                    int tweakPrice = UnityEngine.Random.Range(-100, 101);
                    item.verification.officialDeclaredPriceUSD = Mathf.Max(0, item.declaredPriceUSD + tweakPrice);
                }

                // Recompute signature
                item.verification.signature = ComputeSignature(
                    item.verification.officialDestination,
                    item.verification.officialCategory,
                    item.verification.officialDeclaredWeightKg,
                    item.verification.officialDeclaredPriceUSD
                );
            }

            return item;
        }

        /// <summary>Create randomized rules for the day.</summary>
        public DayParamsData GenerateNewRandomDayRules()
        {
            var day = ScriptableObject.CreateInstance<DayParamsData>();

            // ---------- DESTINATION RULES ----------
            var destRule = new DestinationRule();

            if (Chance(0.30f))
            {
                destRule.whitelistMode = true;
                int allowedCount = 3;
                destRule.allowedDestinations = PickDistinct(DESTINATIONS_POOL, allowedCount);
            }
            else
            {
                int forbidCount = Chance(0.50f) ? 2 : (Chance(0.20f) ? 1 : 0);
                if (forbidCount > 0)
                    destRule.forbiddenDestinations = PickDistinct(DESTINATIONS_POOL, forbidCount);
            }

            day.destinationRule = destRule;

            // ---------- CATEGORY RULES ----------
            var catRule = new CategoryRule();
            if (Chance(0.25f))
            {
                catRule.whitelistMode = true;
                catRule.allowed = PickDistinctEnum<ProductCategory>(2, ProductCategory.Undefined);
            }
            else if (Chance(0.35f))
            {
                catRule.forbidden = PickDistinctEnum<ProductCategory>(1, ProductCategory.Undefined);
            }
            day.categoryRule = catRule;

            // ---------- WEIGHT RULES ----------
            var weightRule = new WeightRule();
            if (Chance(0.5f))
            {
                weightRule.checkDeclared = true;
                weightRule.declaredComparator = Chance(0.5f) ? Comparator.LessEqual : Comparator.GreaterEqual;
                weightRule.declaredKg = RandomRangeRound(4f, 12f, 0.5f);
            }
            if (Chance(0.7f))
            {
                weightRule.checkReal = true;
                weightRule.realComparator = Chance(0.5f) ? Comparator.LessEqual : Comparator.GreaterEqual;
                weightRule.realKg = RandomRangeRound(4f, 12f, 0.5f);
            }
            if (weightRule.checkDeclared && weightRule.checkReal && Chance(0.45f))
            {
                weightRule.requireConsistency = true;
                weightRule.maxAllowedDiffKg = 1.0f;
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

            // ---------- TUNING ----------
            day.maxBeltBuffer = 6;
            day.dayTimeSeconds = UnityEngine.Random.Range(75, 101);

            return day;
        }

        /// <summary>Validate an item against day rules. Returns true if ACCEPTED; violations contains reasons if rejected.</summary>
        public bool EvaluateItemAgainstDay(ItemData item, DayParamsData day, out List<string> violations)
        {
            violations = new List<string>();

            // DESTINATION
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

            // CATEGORY
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

            // PRICE
            if (day.priceRule != null && day.priceRule.enabled)
            {
                if (!Compare(item.declaredPriceUSD, day.priceRule.comparator, day.priceRule.usd))
                    violations.Add($"Price rule failed: {item.declaredPriceUSD} {Describe(day.priceRule.comparator)} {day.priceRule.usd}.");
            }

            // WEIGHT
            if (day.weightRule != null)
            {
                var wr = day.weightRule;
                if (wr.checkDeclared && !Compare(item.declaredWeightKg, wr.declaredComparator, wr.declaredKg))
                    violations.Add($"Declared weight rule failed: {item.declaredWeightKg}kg {Describe(wr.declaredComparator)} {wr.declaredKg}kg.");
                if (wr.checkReal && !Compare(item.realWeightKg, wr.realComparator, wr.realKg))
                    violations.Add($"Real weight rule failed: {item.realWeightKg}kg {Describe(wr.realComparator)} {wr.realKg}kg.");
                if (wr.requireConsistency)
                {
                    float diff = Mathf.Abs(item.realWeightKg - item.declaredWeightKg);
                    if (diff > wr.maxAllowedDiffKg)
                        violations.Add($"Weight consistency failed: |real-declared|={diff:0.0}kg > {wr.maxAllowedDiffKg}kg.");
                }
            }

            // HIDDEN attributes
            if (day.attributeBanRule != null && day.attributeBanRule.forbiddenHiddenFlags != HiddenFlags.None)
            {
                var banned = day.attributeBanRule.forbiddenHiddenFlags;
                if ((item.hiddenFlags & banned) != 0)
                    violations.Add($"Hidden attribute banned: {(item.hiddenFlags & banned)}.");
            }

            return violations.Count == 0;
        }

        // ======================================================
        //                     HELPERS
        // ======================================================

        private string PickDifferentDestination(string current)
        {
            string pick = current;
            int guard = 32;
            while (pick == current && guard-- > 0)
                pick = DESTINATIONS_POOL[UnityEngine.Random.Range(0, DESTINATIONS_POOL.Length)];
            return pick;
        }

        private ProductCategory RandomDifferentCategory(ProductCategory current)
        {
            var values = (ProductCategory[])Enum.GetValues(typeof(ProductCategory));
            ProductCategory pick = current;
            int guard = 32;
            while (pick == current && guard-- > 0)
            {
                pick = values[UnityEngine.Random.Range(0, values.Length)];
                if (pick == ProductCategory.Undefined) pick = ProductCategory.Other;
            }
            return pick;
        }

        private string ComputeSignature(string dest, ProductCategory cat, float declaredW, int price)
        {
            string payload = $"{dest}|{(int)cat}|{declaredW:0.0}|{price}";
            int h = Animator.StringToHash(payload);
            return h.ToString("X8");
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

        static bool Compare(int a, Comparator c, int b)
        {
            switch (c)
            {
                case Comparator.LessEqual:    return a <= b;
                case Comparator.GreaterEqual: return a >= b;
                case Comparator.Equal:        return a == b;
                case Comparator.NotEqual:     return a != b;
                default:                      return true;
            }
        }

        static bool Compare(float a, Comparator c, float b)
        {
            switch (c)
            {
                case Comparator.LessEqual:    return a <= b + 1e-4f;
                case Comparator.GreaterEqual: return a >= b - 1e-4f;
                case Comparator.Equal:        return Mathf.Abs(a - b) < 1e-3f;
                case Comparator.NotEqual:     return Mathf.Abs(a - b) >= 1e-3f;
                default:                      return true;
            }
        }

        static string Describe(Comparator c)
        {
            return c switch
            {
                Comparator.LessEqual    => "<=",
                Comparator.GreaterEqual => ">=",
                Comparator.Equal        => "==",
                Comparator.NotEqual     => "!=",
                _                       => "(any)"
            };
        }

        static Color RandomPastel()
        {
            float h = UnityEngine.Random.value;
            float s = 0.45f;
            float v = 0.95f;
            Color c = Color.HSVToRGB(h, s, v);
            return c;
        }
    }
}
