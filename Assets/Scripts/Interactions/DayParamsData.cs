using System.Collections.Generic;
using UnityEngine;
using System;

namespace ProductsPlease.Interactions
{
    
    public enum Comparator
    {
        Any,
        LessEqual,
        GreaterEqual,
        Equal,
        NotEqual
    }

    [Serializable]
    public class WeightRule
    {
        [Tooltip("Compare against DECLARED weight on label.")]
        public bool checkDeclared = false;

        public Comparator declaredComparator = Comparator.Any;
        public float declaredKg = 0f;

        [Tooltip("Compare against REAL weight on scale.")]
        public bool checkReal = false;

        public Comparator realComparator = Comparator.Any;
        public float realKg = 0f;

        [Tooltip("If both checks are active, you can also require label/real consistency.")]
        public bool requireConsistency = false;

        public float maxAllowedDiffKg = 0.5f;
    }

    [Serializable]
    public class PriceRule
    {
        public bool enabled = false;
        public Comparator comparator = Comparator.LessEqual;
        public int usd = 500;
    }

    [Serializable]
    public class DestinationRule
    {
        [Tooltip("Explicitly forbidden destinations for the current day.")]
        public List<string> forbiddenDestinations = new();

        [Tooltip("If true, ONLY these destinations are allowed.")]
        public bool whitelistMode = false;

        public List<string> allowedDestinations = new();

        [Tooltip("If true, packages with these destinations must be scanned.")]
        public List<string> requireScanForDestinations = new();
    }

    [Serializable]
    public class CategoryRule
    {
        [Tooltip("If true, ONLY the listed categories are accepted.")]
        public bool whitelistMode = false;

        public List<ProductCategory> allowed = new();

        [Tooltip("Categories that are explicitly forbidden.")]
        public List<ProductCategory> forbidden = new();
    }

    [Serializable]
    public class AttributeBanRule
    {
        [Tooltip("Any hidden flag listed here will automatically cause rejection.")]
        public HiddenFlags forbiddenHiddenFlags = HiddenFlags.None;
    }

    [Serializable]
    public class ProcessRule
    {
        [Header("— Global process requirements —")]
        public bool requireWeighAll = false;

        public bool requireScanAll = false;
        public bool requireMagneticCheck = false;
        public bool requireRadiationCheck = false;
        public bool requireChemicalCheck = false;

        [Header("— Conditional checks —")] public bool ifInternationalRequireScan = false;
        public bool ifElectronicsRequireMagnet = false;
        public bool ifMedicalRequireChemical = false;
        public bool ifRealHeavierThanDeclaredRequireRadiation = false;
    }

    [CreateAssetMenu(menuName = "Game/DayParamsData", fileName = "DayParams_")]
    public class DayParamsData : ScriptableObject
    {
        [Header("— Visible label rules —")] public DestinationRule destinationRule = new();
        public CategoryRule categoryRule = new();
        public WeightRule weightRule = new();
        public PriceRule priceRule = new();

        [Header("— Hidden attribute bans —")] public AttributeBanRule attributeBanRule = new();

        [Header("— Process / station requirements —")]
        public ProcessRule processRule = new();

        [Header("— Gameplay tuning —")] [Tooltip("Max number of packages allowed on the belt before penalty.")]
        public int maxBeltBuffer = 6;

        [Tooltip("Duration of the day in seconds.")]
        public int dayTimeSeconds = 90;

        [Tooltip("Max number of stations allowed per package (0 = no limit).")]
        public int maxStationsPerItem = 0;

        [Header("— Designer notes —")] [TextArea]
        public string designerNotes;
    }
}