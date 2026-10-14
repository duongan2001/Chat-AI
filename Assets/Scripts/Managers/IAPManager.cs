using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;

public class IAPManager : MonoBehaviour, IStoreListener
{
    public static IAPManager Instance;

    private IStoreController m_Controller;

    private IAppleExtensions m_AppleExtensions;

    // private ISamsungAppsExtensions m_SamsungExtensions;
    //private IMicrosoftExtensions m_MicrosoftExtensions;
    //private ITransactionHistoryExtensions m_TransactionHistoryExtensions;
    private IGooglePlayStoreExtensions m_GooglePlayStoreExtensions;

    private bool m_PurchaseInProgress;

    private Action<PurchaseEventArgs> SucessEvent;
    private Action<Product, PurchaseFailureReason> FailEvent;

    public bool isSubscriptionPurchased, isRemoveAdPurchased;
    public string subTimeUnit = "d";
    public string currentWeeklySubPrice;
    public string currentYearlySubPrice;

    public bool isInitialized { get; private set; } = false;

    public void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        var module = StandardPurchasingModule.Instance();

        // The FakeStore supports: no-ui (always succeeding), basic ui (purchase pass/fail), and
        // developer ui (initialization, purchase, failure code setting). These correspond to
        // the FakeStoreUIMode Enum values passed into StandardPurchasingModule.useFakeStoreUIMode.
        module.useFakeStoreUIMode = FakeStoreUIMode.StandardUser;

        var builder = ConfigurationBuilder.Instance(module);

        // Set this to true to enable the Microsoft IAP simulator for local testing.
        //builder.Configure<IMicrosoftConfiguration>().useMockBillingSystem = false;

#if AGGRESSIVE_INTERRUPT_RECOVERY_GOOGLEPLAY
        // For GooglePlay, if we have access to a backend server to deduplicate purchases, query purchase history
        // when attempting to recover from a network-interruption encountered during purchasing. Strongly recommend
        // deduplicating transactions across app reinstallations because this relies upon the on-device, deletable
        // TransactionLog database.
        builder.Configure<IGooglePlayConfiguration>().aggressivelyRecoverLostPurchases = true;
        // Use purchaseToken instead of orderId for all transactions to avoid non-unique transactionIDs for a
        // single purchase; two ProcessPurchase calls for one purchase, differing only by which field of the receipt
        // is used for the Product.transactionID. Automatically true if aggressivelyRecoverLostPurchases is enabled
        // and this API is not called at all.
        builder.Configure<IGooglePlayConfiguration>().UsePurchaseTokenForTransactionId(true);
#endif

        // Define our products.
        // Either use the Unity IAP Catalog, or manually use the ConfigurationBuilder.AddProduct API.
        // Use IDs from both the Unity IAP Catalog and hardcoded IDs via the ConfigurationBuilder.AddProduct API.

        // Use the products defined in the IAP Catalog GUI.
        // E.g. Menu: "Window" > "Unity IAP" > "IAP Catalog", then add products, then click "App Store Export".
        //var catalog = ProductCatalog.LoadDefaultCatalog();

        //foreach (var product in catalog.allValidProducts)
        //{
        //    if (product.allStoreIDs.Count > 0)
        //    {
        //        var ids = new IDs();
        //        foreach (var storeID in product.allStoreIDs)
        //        {
        //            ids.Add(storeID.id, storeID.store);
        //        }
        //        builder.AddProduct(product.id, product.type, ids);
        //    }
        //    else
        //    {
        //        builder.AddProduct(product.id, product.type);
        //    }
        //}

        builder.AddProduct("buy_rose_100", ProductType.Consumable);
        builder.AddProduct("buy_rose_200", ProductType.Consumable);
        builder.AddProduct("buy_rose_500", ProductType.Consumable);
        builder.AddProduct("buy_rose_1000", ProductType.Consumable);
        builder.AddProduct("buy_rose_2000", ProductType.Consumable);
        builder.AddProduct("buy_rose_4000", ProductType.Consumable);

        builder.AddProduct("weeklyid", ProductType.Subscription);
        builder.AddProduct("yearlyid", ProductType.Subscription);
        // builder.Configure<ISamsungAppsConfiguration>().SetMode(SamsungAppsMode.AlwaysSucceed);

#if INTERCEPT_PROMOTIONAL_PURCHASES
        // On iOS and tvOS we can intercept promotional purchases that come directly from the App Store.
        // On other platforms this will have no effect; OnPromotionalPurchase will never be called.
        builder.Configure<IAppleConfiguration>().SetApplePromotionalPurchaseInterceptorCallback(OnPromotionalPurchase);
        Debug.Log("Setting Apple promotional purchase interceptor callback");
#endif

#if RECEIPT_VALIDATION
        string appIdentifier;
#if UNITY_5_6_OR_NEWER
        appIdentifier = Application.identifier;
#else
        appIdentifier = Application.bundleIdentifier;
#endif
        validator = new CrossPlatformValidator(GooglePlayTangle.Data(), AppleTangle.Data(), appIdentifier);
#endif

        UnityPurchasing.Initialize(this, builder);
    }

    public Product GetProduct(string productID)
    {
        if (m_Controller == null)
        {
            Debug.LogWarning("Purchasing is not initialized");
            return null;
        }

        if (m_Controller.products.WithID(productID) == null)
        {
            Debug.LogWarning("No product has id " + productID);
            return null;
        }

        var product = m_Controller.products.WithID(productID);
        return product;
    }

    public void PurchaseButtonClick(string productId, Action<PurchaseEventArgs> sucessEvent = null,
        Action<Product, PurchaseFailureReason> failEvent = null)
    {
        if (m_PurchaseInProgress == true)
        {
            Debug.Log("Please wait, purchase in progress");
            return;
        }

        if (m_Controller == null)
        {
            Debug.LogError("Purchasing is not initialized");
            return;
        }

        if (m_Controller.products.WithID(productId) == null)
        {
            Debug.LogError("No product has id " + productId);
            return;
        }

        // Don't need to draw our UI whilst a purchase is in progress.
        // This is not a requirement for IAP Applications but makes the demo
        // scene tidier whilst the fake purchase dialog is showing.
        m_PurchaseInProgress = true;
        SucessEvent = sucessEvent;
        FailEvent = failEvent;

        //Sample code how to add accountId in developerPayload to pass it to getBuyIntentExtraParams
        //Dictionary<string, string> payload_dictionary = new Dictionary<string, string>();
        //payload_dictionary["accountId"] = "Faked account id";
        //payload_dictionary["developerPayload"] = "Faked developer payload";
        //m_Controller.InitiatePurchase(m_Controller.products.WithID(productID), MiniJson.JsonEncode(payload_dictionary));
        m_Controller.InitiatePurchase(m_Controller.products.WithID(productId));
    }

    public bool CheckHasSub(string productId)
    {
#if UNITY_EDITOR
        return PlayerPrefs.GetInt(productId, 0) == 1;
#endif

        if (m_Controller == null)
            return false;

        var item = m_Controller.products.WithID(productId);
        if (item == null)
            return false;

        // ⚠️ QUAN TRỌNG: phải check receipt thật
        if (string.IsNullOrEmpty(item.receipt))
        {
            Debug.LogWarning("No receipt for: " + productId);
            return false;
        }

        try
        {
            if (item.definition.type == ProductType.Subscription)
            {
                var subManager = new SubscriptionManager(item, null);
                var info = subManager.getSubscriptionInfo();

                return info.IsSubscribed() == Result.True &&
                       info.IsExpired() == Result.False;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("CheckHasSub ERROR: " + e);
        }

        return false;
    }

    public bool IsNoAdsPurchased()
    {
#if UNITY_EDITOR
        return PlayerPrefs.GetInt("remove_ads", 0) == 1;
#endif

        if (m_Controller == null)
            return false;

        var item = m_Controller.products.WithID("remove_ads");
        if (item == null)
            return false;

        return item.hasReceipt;
    }

    public int GetSubscriptionTimeRemaining(string productId)
    {
        subTimeUnit = "d";

#if UNITY_EDITOR
        subTimeUnit = "d";
        return PlayerPrefs.GetInt(productId, 0) == 1 ? 7 : 0;
#endif

        if (m_Controller == null)
            return 0;

        var item = m_Controller.products.WithID(productId);
        if (item == null || string.IsNullOrEmpty(item.receipt))
            return 0;

        try
        {
            if (item.definition.type == ProductType.Subscription)
            {
                var subManager = new SubscriptionManager(item, null);
                var info = subManager.getSubscriptionInfo();

                if (info.IsSubscribed() != Result.True || info.IsExpired() == Result.True)
                    return 0;

                TimeSpan remaining = info.getRemainingTime();

                if (remaining.TotalDays >= 1)
                {
                    subTimeUnit = "d";
                    return (int)Math.Ceiling(remaining.TotalDays);
                }
                else if (remaining.TotalHours >= 1)
                {
                    subTimeUnit = "h";
                    return (int)Math.Ceiling(remaining.TotalHours);
                }
                else
                {
                    subTimeUnit = "s";
                    return Mathf.Max(0, (int)remaining.TotalSeconds);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("GetSubscriptionTimeRemaining ERROR: " + e);
        }

        return 0;
    }

    private void OnDeferred(Product item)
    {
        Debug.Log("Purchase deferred: " + item.definition.id);
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        Debug.LogWarning("Initializing IAP!");

        m_Controller = controller;
        m_AppleExtensions = extensions.GetExtension<IAppleExtensions>();
        // m_SamsungExtensions = extensions.GetExtension<ISamsungAppsExtensions>();
        //m_MicrosoftExtensions = extensions.GetExtension<IMicrosoftExtensions>();
        //m_TransactionHistoryExtensions = extensions.GetExtension<ITransactionHistoryExtensions>();
        //m_GooglePlayStoreExtensions = extensions.GetExtension<IGooglePlayStoreExtensions>();
        // m_GooglePlayStoreExtensions.SetLogLevel(0); // 0 == debug, info, warning, error. 1 == warning, error only.

        // On Apple platforms we need to handle deferred purchases caused by Apple's Ask to Buy feature.
        // On non-Apple platforms this will have no effect; OnDeferred will never be called.
        m_AppleExtensions.RegisterPurchaseDeferredListener(OnDeferred);

#if SUBSCRIPTION_MANAGER
        Dictionary<string, string> introductory_info_dict = m_AppleExtensions.GetIntroductoryPriceDictionary();
#endif
        // Sample code for expose product sku details for apple store
        //Dictionary<string, string> product_details = m_AppleExtensions.GetProductDetails();

#if UNITY_EDITOR
        Debug.Log("Available items:");
        foreach (var item in controller.products.all)
        {
            if (item.availableToPurchase)
            {
                Debug.Log(string.Join(" - ",
                    new[]
                    {
                        item.metadata.localizedTitle,
                        item.metadata.localizedDescription,
                        item.metadata.isoCurrencyCode,
                        item.metadata.localizedPrice.ToString(),
                        item.metadata.localizedPriceString,
                        item.transactionID,
                        item.receipt
                    }));
#if INTERCEPT_PROMOTIONAL_PURCHASES
                // Set all these products to be visible in the user's App Store according to Apple's Promotional IAP feature
                // https://developer.apple.com/library/content/documentation/NetworkingInternet/Conceptual/StoreKitGuide/PromotingIn-AppPurchases/PromotingIn-AppPurchases.html
                m_AppleExtensions.SetStorePromotionVisibility(item, AppleStorePromotionVisibility.Show);
#endif

#if SUBSCRIPTION_MANAGER
                // this is the usage of SubscriptionManager class
                if (item.receipt != null) {
                    if (item.definition.type == ProductType.Subscription) {
                        if (checkIfProductIsAvailableForSubscriptionManager(item.receipt)) {
                            string intro_json =
 (introductory_info_dict == null || !introductory_info_dict.ContainsKey(item.definition.storeSpecificId)) ? null : introductory_info_dict[item.definition.storeSpecificId];
                            SubscriptionManager p = new SubscriptionManager(item, intro_json);
                            SubscriptionInfo info = p.getSubscriptionInfo();
                            Debug.Log("product id is: " + info.getProductId());
                            Debug.Log("purchase date is: " + info.getPurchaseDate());
                            Debug.Log("subscription next billing date is: " + info.getExpireDate());
                            Debug.Log("is subscribed? " + info.isSubscribed().ToString());
                            Debug.Log("is expired? " + info.isExpired().ToString());
                            Debug.Log("is cancelled? " + info.isCancelled());
                            Debug.Log("product is in free trial peroid? " + info.isFreeTrial());
                            Debug.Log("product is auto renewing? " + info.isAutoRenewing());
                            Debug.Log("subscription remaining valid time until next billing date is: " + info.getRemainingTime());
                            Debug.Log("is this product in introductory price period? " + info.isIntroductoryPricePeriod());
                            Debug.Log("the product introductory localized price is: " + info.getIntroductoryPrice());
                            Debug.Log("the product introductory price period is: " + info.getIntroductoryPricePeriod());
                            Debug.Log("the number of product introductory price period cycles is: " + info.getIntroductoryPricePeriodCycles());
                        } else {
                            Debug.Log("This product is not available for SubscriptionManager class, only products that are purchase by 1.19+ SDK can use this class.");
                        }
                    } else {
                        Debug.Log("the product is not a subscription product");
                    }
                } else {
                    Debug.Log("the product should have a valid receipt");
                }
#endif
            }
        }
#endif
        isRemoveAdPurchased = IsNoAdsPurchased();
        isSubscriptionPurchased = CheckHasSub("weeklyid") || CheckHasSub("yearlyid");

        var weeklyProduct = m_Controller.products.WithID("weeklyid");
        if (weeklyProduct != null)
            currentWeeklySubPrice = weeklyProduct.metadata.localizedPriceString;

        var yearlyProduct = m_Controller.products.WithID("yearlyid");
        if (yearlyProduct != null)
            currentYearlySubPrice = yearlyProduct.metadata.localizedPriceString;

        Debug.Log($"isRemoveAdPurchased: {isRemoveAdPurchased}");
        Debug.Log($"isSubscriptionPurchased: {isSubscriptionPurchased}");

        isInitialized = true;
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.Log("Billing failed to initialize!");
        switch (error)
        {
            case InitializationFailureReason.AppNotKnown:
                Debug.LogError("Is your App correctly uploaded on the relevant publisher console?");
                break;
            case InitializationFailureReason.PurchasingUnavailable:
                // Ask the user if billing is disabled in device settings.
                Debug.Log("Billing disabled!");
                break;
            case InitializationFailureReason.NoProductsAvailable:
                // Developer configuration error; check product metadata.
                Debug.Log("No products available for purchase!");
                break;
        }
    }

    public void OnPurchaseFailed(Product i, PurchaseFailureReason p)
    {
        m_PurchaseInProgress = false;

        FailEvent?.Invoke(i, p);
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
    {
        m_PurchaseInProgress = false;

        // Cập nhật lại sau khi mua
        var productId = e.purchasedProduct.definition.id;
        if (productId == "remove_ads")
        {
            isRemoveAdPurchased = true;
            PlayerPrefs.SetInt("no_ads", 1); // cache offline
        }
        else if (productId == "weeklyid")
        {
            isSubscriptionPurchased = CheckHasSub("weeklyid");
        }
        else if (productId == "yearlyid")
        {
            isSubscriptionPurchased = CheckHasSub("yearlyid");
        }

        SucessEvent?.Invoke(e);

        return PurchaseProcessingResult.Complete;
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        throw new NotImplementedException();
    }
}