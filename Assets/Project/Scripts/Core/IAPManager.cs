using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using AIStartupTycoon.Data;
using AIStartupTycoon.Utils;

namespace AIStartupTycoon.Core
{
    /// <summary>
    /// Wraps Unity IAP (v5 StoreController API - the classic IStoreListener API is
    /// deprecated as of the package version this project uses). Initializes the store from
    /// the IAPProductData catalog, routes successful purchases to the same reward
    /// vocabulary (cash/Reputation/permanent multiplier) DailyLoginManager and QuestManager
    /// already use, and tracks which non-consumables are owned. Product IDs here must
    /// exactly match IDs configured in the Google Play Console (and any other store
    /// back-end) - this class only reacts to a purchase succeeding, it never creates or
    /// prices products on the store's side.
    /// </summary>
    public class IAPManager : MonoBehaviour
    {
        public static IAPManager Instance { get; private set; }

        [Header("Catalog")]
        public List<IAPProductData> allProducts;

        public bool IsInitialized { get; private set; }
        public bool AdsRemoved { get; private set; }

        public event Action OnStoreInitialized;
        public event Action<IAPProductData> OnPurchaseCompleted;
        public event Action<IAPProductData, string> OnPurchaseFailedEvent; // (product, reason)

        private StoreController _storeController;
        private readonly Dictionary<string, Product> _fetchedProducts = new Dictionary<string, Product>();
        private readonly HashSet<string> _ownedNonConsumableIds = new HashSet<string>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private async void Start()
        {
            await InitializePurchasing();
        }

        private async System.Threading.Tasks.Task InitializePurchasing()
        {
            if (allProducts == null || allProducts.Count == 0) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            // Real store connection is Android-only (see NotificationManager/
            // RatingPromptManager, both Play-Store-specific) and guarded out of the Editor
            // for the same reason NotificationManager guards its Android APIs: requesting
            // the GooglePlay store constructs the real Billing AAR client, which needs a
            // live Android JNI activity and throws immediately outside a real device/
            // emulator build - there's no in-Editor fake-store equivalent for the v5 API
            // this project uses. Switch to AppleAppStore.Name or add a second
            // StoreController if iOS support is ever added.
            _storeController = UnityIAPServices.StoreController(GooglePlay.Name);
            _storeController.OnPurchasePending += OnPurchasePending;
            _storeController.OnPurchasesFetched += OnPurchasesFetched;
            _storeController.OnPurchaseFailed += OnPurchaseFailedInternal;
            _storeController.OnProductsFetched += OnProductsFetched;
            _storeController.OnProductsFetchFailed += OnProductsFetchFailed;

            await _storeController.Connect();

            var defs = new List<ProductDefinition>();
            foreach (var product in allProducts)
            {
                if (product == null || string.IsNullOrEmpty(product.productId)) continue;
                ProductType type = product.kind == IAPProductKind.Consumable ? ProductType.Consumable : ProductType.NonConsumable;
                defs.Add(new ProductDefinition(product.productId, type));
            }
            _storeController.FetchProductsWithNoRetries(defs);
#else
            await System.Threading.Tasks.Task.CompletedTask;
            Debug.Log("[IAPManager] Store connection skipped (Editor/non-Android). Rows will show fallback prices; Buy stays disabled.");
#endif
        }

        private void OnProductsFetched(List<Product> products)
        {
            foreach (var p in products) _fetchedProducts[p.definition.id] = p;
            IsInitialized = true;

            // Restore non-consumable ownership from what the store itself reports - more
            // trustworthy than only our own save file, since it also covers a fresh
            // install / restored purchase on a new device.
            _storeController.FetchPurchases();

            OnStoreInitialized?.Invoke();
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            Debug.LogWarning($"[IAPManager] Product fetch failed: {failure.FailureReason}");
        }

        private void OnPurchasesFetched(Orders orders)
        {
            foreach (var confirmedOrder in orders.ConfirmedOrders)
            {
                foreach (var item in confirmedOrder.CartOrdered.Items())
                {
                    IAPProductData product = allProducts.FirstOrDefault(p => p != null && p.productId == item.Product.definition.id);
                    if (product != null && product.kind == IAPProductKind.NonConsumable)
                        MarkOwned(product);
                }
            }
        }

        /// <summary>Call from a Buy button. Does nothing if the store isn't ready yet or the
        /// product is a non-consumable that's already owned.</summary>
        public void BuyProduct(IAPProductData product)
        {
            if (!IsInitialized || product == null) return;
            if (product.kind == IAPProductKind.NonConsumable && IsOwned(product)) return;

            _storeController.PurchaseProduct(product.productId);
        }

        private void OnPurchasePending(PendingOrder order)
        {
            IAPProductData product = allProducts.FirstOrDefault(p => p != null && p.productId == order.CartOrdered.Items().First().Product.definition.id);
            if (product != null)
            {
                GrantReward(product);
                if (product.kind == IAPProductKind.NonConsumable) MarkOwned(product);

                OnPurchaseCompleted?.Invoke(product);
                if (GameManager.Instance != null) GameManager.Instance.SaveGame();
            }
            else
            {
                Debug.LogWarning("[IAPManager] Received a pending order for an unrecognized product.");
            }

            _storeController.ConfirmPurchase(order);
        }

        private void OnPurchaseFailedInternal(FailedOrder failedOrder)
        {
            string productId = failedOrder.CartOrdered.Items().First().Product.definition.id;
            IAPProductData data = allProducts.FirstOrDefault(p => p != null && p.productId == productId);
            OnPurchaseFailedEvent?.Invoke(data, failedOrder.Details);
            Debug.LogWarning($"[IAPManager] Purchase failed for '{productId}': {failedOrder.FailureReason} - {failedOrder.Details}");
        }

        private void GrantReward(IAPProductData product)
        {
            if (product.cashReward > 0)
                CurrencyManager.Instance.GrantCash(new BigNumber(product.cashReward, 0));

            if (product.reputationReward > 0)
                CurrencyManager.Instance.GrantReputation(product.reputationReward);

            if (product.permanentEarningsMultiplier != 1.0)
                CurrencyManager.Instance.ReputationMultiplier *= product.permanentEarningsMultiplier;

            if (product.removesAds)
            {
                AdsRemoved = true;
                if (GameManager.Instance != null) GameManager.Instance.SetAdsRemoved(true);
            }
        }

        private void MarkOwned(IAPProductData product) => _ownedNonConsumableIds.Add(product.productId);

        public bool IsOwned(IAPProductData product) => product != null && _ownedNonConsumableIds.Contains(product.productId);

        /// <summary>Localized price from the store if it's been fetched yet, otherwise the
        /// asset's authored fallback text (e.g. so the shop row has something to show
        /// before the store finishes initializing).</summary>
        public string GetPriceString(IAPProductData product)
        {
            if (product != null && _fetchedProducts.TryGetValue(product.productId, out Product storeProduct)
                && !string.IsNullOrEmpty(storeProduct.metadata.localizedPriceString))
                return storeProduct.metadata.localizedPriceString;

            return product != null ? product.fallbackPriceText : "";
        }

        // --- Save/Load support (called from GameManager, which owns the save file) ---

        public List<string> GetOwnedProductIds() => _ownedNonConsumableIds.ToList();

        public void LoadOwnedProductIds(List<string> ids)
        {
            _ownedNonConsumableIds.Clear();
            if (ids == null) return;
            foreach (var id in ids) _ownedNonConsumableIds.Add(id);

            AdsRemoved = allProducts != null && allProducts.Any(p => p != null && p.removesAds && _ownedNonConsumableIds.Contains(p.productId));
        }
    }
}
