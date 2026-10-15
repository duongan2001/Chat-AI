
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

public class IAPPuchase : MonoBehaviour
{
    #region VARIABLES

    [Header("--- PRODUCT DATA ---")]
    public float priceInUsd;
    public string productId;
    public Text priceText;

    #endregion


    #region UNITY METHODS

    public void Start()
    {
        CheckProduct();
    }

    #endregion


    #region PRODUCT METHODS

    public void CheckProduct()
    {
        if (IAPManager.Instance == null)
        {
            SetFallbackPrice();
            return;
        }

        Product product = IAPManager.Instance.GetProduct(productId);

        if (product != null && product.metadata != null &&
            !string.IsNullOrEmpty(product.metadata.localizedPriceString))
        {
            if (priceText != null)
                priceText.text = product.metadata.localizedPriceString;
        }
        else
        {
            SetFallbackPrice();
        }
    }

    private void SetFallbackPrice()
    {
        if (priceText != null)
            priceText.text = "$" + priceInUsd.ToString("0.##");
    }

    #endregion


    #region PURCHASE METHODS

    public void BuyButtonPress()
    {
        if (IAPManager.Instance == null)
        {
            Debug.LogError("[IAP] IAPManager is null.");
            return;
        }

        switch (productId)
        {
            case "buy_rose_100":
            case "buy_rose_200":
            case "buy_rose_500":
            case "buy_rose_1000":
            case "buy_rose_2000":
            case "buy_rose_4000":
                IAPManager.Instance.PurchaseButtonClick(productId, PurchaseRoseSuccessful);
                break;

            case "weeklyid":
            case "yearlyid":
                IAPManager.Instance.PurchaseButtonClick(productId, PurchaseSubscriptionSuccessful);
                break;

            default:
                Debug.LogWarning("[IAP] Unknown Product ID: " + productId);
                break;
        }
    }

    #endregion


    #region ROSE PURCHASE METHODS

    public void PurchaseRoseSuccessful(PurchaseEventArgs args)
    {
        if (args == null || args.purchasedProduct == null)
            return;

        string purchasedId = args.purchasedProduct.definition.id;

        int roseAmount = GetRoseAmount(purchasedId);

        if (roseAmount <= 0)
        {
            Debug.LogWarning("[IAP] Invalid Rose Product: " + purchasedId);
            return;
        }

        Debug.Log("[IAP] Purchased: " + purchasedId + " | Roses: " + roseAmount);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateRoseAmount(roseAmount);

            int packageIndex = GetRosePackageIndex(purchasedId);

            if (packageIndex >= 0 && UIManager.Instance.roseShopPanel != null)
            {
                UIManager.Instance.roseShopPanel.PlayRosePurchaseEffect(packageIndex);
            }
        }
        else
        {
            string key = PlayerPrefKey.NumberOfRose.ToString();

            int currentRoses = PlayerPrefs.GetInt(key, 0);

            long total = (long)currentRoses + roseAmount;

            PlayerPrefs.SetInt(key, (int)System.Math.Min(total, int.MaxValue));
            PlayerPrefs.Save();
        }
    }

    private int GetRoseAmount(string id)
    {
        switch (id)
        {
            case "buy_rose_100": return 100;
            case "buy_rose_200": return 200 + 10;
            case "buy_rose_500": return 500 + 50;
            case "buy_rose_1000": return 1000 + 200;
            case "buy_rose_2000": return 2000 + 400;
            case "buy_rose_4000": return 4000 + 1200;
            default: return 0;
        }
    }

    private int GetRosePackageIndex(string id)
    {
        switch (id)
        {
            case "buy_rose_4000": return 0;
            case "buy_rose_2000": return 1;
            case "buy_rose_1000": return 2;
            case "buy_rose_500": return 3;
            case "buy_rose_200": return 4;
            case "buy_rose_100": return 5;
            default: return -1;
        }
    }

    #endregion


    #region SUBSCRIPTION PURCHASE METHODS

    public void PurchaseSubscriptionSuccessful(PurchaseEventArgs args)
    {
        if (args == null || args.purchasedProduct == null)
            return;

        string purchasedId = args.purchasedProduct.definition.id;

        if (purchasedId != "weeklyid" && purchasedId != "yearlyid")
            return;

        Debug.Log("[IAP] Subscription purchased: " + purchasedId);

        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.isSubscriptionPurchased = true;
        }

        // Refresh subscription-related UI here if needed.
    }

    #endregion
}
