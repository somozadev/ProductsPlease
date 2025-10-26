using System;
using UnityEngine;

namespace ProductsPlease.Interactions
{
    public enum ProductCategory
    {
        Undefined,
        Food,
        Medical,
        Electronics,
        Machinery,
        Chemicals,
        Documents,
        Gifts,
        Other
    }

    [Flags]
    public enum HiddenFlags
    {
        None          = 0,
        IsMetallic    = 1 << 0, // Magnetic detector
        IsRadioactive = 1 << 1, // Radiation detector
        IsChemical    = 1 << 2, // Chemical detector
        FakeLabel     = 1 << 3  // Barcode data doesn't match visible label (when Info scan verifies)
    }

    /// <summary>
    /// Data used by the Info Sensor to verify if the visible label matches the "official" record.
    /// </summary>
    [Serializable]
    public struct VerificationData
    {
        public string officialDestination;
        public ProductCategory officialCategory;
        public float officialDeclaredWeightKg;
        public int officialDeclaredPriceUSD;
        public string signature; // optional checksum for display
    }

    [CreateAssetMenu(menuName = "Game/ItemData", fileName = "ItemData_")]
    public class ItemData : ScriptableObject
    {
        [Header("— Visible label data —")]
        public string itemId;
        public string destination;                 // City or country (visible on label)
        public ProductCategory productCategory;    // Declared product type
        public float declaredWeightKg = 1f;        // Visible declared weight
        public int declaredPriceUSD = 0;           // Declared price
        public string barcode;                     // Read by Info scanner

        [Header("— Hidden / detectable properties —")]
        public HiddenFlags hiddenFlags;            // Combination of detector flags

        [Header("— Real physical data —")]
        public float realWeightKg = 1f;            // Actual measured on scale

        [Header("— Visual / flavor —")]
        public string displayName;                 // Shown in UI
        public Color tint = Color.white;           // For visual variety

        [Header("— Verification (official) —")]
        public VerificationData verification;      // Official fields used by Info scan
    }
}
