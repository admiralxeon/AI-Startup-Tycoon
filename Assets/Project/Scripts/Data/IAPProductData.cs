using UnityEngine;

namespace AIStartupTycoon.Data
{
    public enum IAPProductKind
    {
        /// <summary>Bought once, owned forever (remove ads, a permanent boost, a bundle).</summary>
        NonConsumable,
        /// <summary>Can be bought repeatedly (a cash pack).</summary>
        Consumable
    }

    /// <summary>
    /// One entry in the real-money store. "productId" must exactly match a product ID
    /// configured in the Google Play Console (and any other store back-end) - this asset
    /// only describes what the game does when that ID is successfully purchased, it does
    /// NOT create or price the product on the store's side.
    /// </summary>
    [CreateAssetMenu(fileName = "IAPProduct_", menuName = "AIStartupTycoon/IAPProduct")]
    public class IAPProductData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Must exactly match the Product ID configured in the Google Play Console.")]
        public string productId;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public IAPProductKind kind;

        [Header("Fallback Price (shown only if the store hasn't returned localized pricing yet)")]
        public string fallbackPriceText = "$0.99";

        [Header("Reward (each optional - leave at 0 / 1.0 / false to skip)")]
        [Tooltip("Free cash granted instantly on successful purchase. Counts toward LifetimeRevenue like any other income.")]
        public double cashReward = 0;
        [Tooltip("Free Reputation (prestige currency) granted instantly on successful purchase.")]
        public double reputationReward = 0;
        [Tooltip("Permanent multiplier applied to CurrencyManager.ReputationMultiplier - same permanent-and-survives-IPO mechanism ReputationUpgradeData/AchievementData use. 1.0 = no effect.")]
        public double permanentEarningsMultiplier = 1.0;
        [Tooltip("If true, this purchase permanently removes ads (sets GameManager's AdsRemoved flag). Only meaningful on a NonConsumable product.")]
        public bool removesAds = false;
    }
}
