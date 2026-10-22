using System.Collections;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

public class SubPanelUIManager : PanelBaseUIController
{
    #region VARIABLES

    [Header("--- SUB PACK BUTTONS ---")]
    [SerializeField] private Image[] subPackButtonImages;
    [SerializeField] private Sprite pickingSubPackSprite;
    [SerializeField] private Sprite notPickingSubPackSprite;

    [Header("--- SUB SETUP ---")]
    [SerializeField] private IAPPuchase sub;

    [Header("--- PRICE TEXT ---")]
    [SerializeField] private Text priceText;

    [Header("--- MULTI LANGUAGE TEXTS ---")]
    [SerializeField] private Text[] yearlyTexts;
    [SerializeField] private Text[] weeklyTexts;

    [Header("--- DEFAULT SETTINGS ---")]
    [SerializeField] private bool defaultToYearly = true;

    private bool isFirstShow = true;
    private bool isPanelShown;
    private string selectedSubId = "yearlyid";

    [Header("--- AD SETTINGS ---")]
    [SerializeField] private float adDelayAfterClose = 0.25f;

    private Coroutine showOpenAdCoroutine;
    private Coroutine showBannerAdCoroutine;

    #endregion

    #region UNITY METHODS

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        selectedSubId = defaultToYearly ? "yearlyid" : "weeklyid";
        ApplySelectedPack();
    }

    #endregion

    #region PANEL METHODS

    public override void Show()
    {
        base.Show();

        RefreshOnShow();
        HideBannerAd();
    }

    public override void ShowDelay()
    {
        base.ShowDelay();

        RefreshOnShow();
        HideBannerAd();
    }

    public override void Hide()
    {
        bool wasPanelShown = isPanelShown;

        base.Hide();

        if (!wasPanelShown)
            return;

        isPanelShown = false;

        StopAdCoroutines();

        if (isFirstShow)
        {
            isFirstShow = false;

            if (UIManager.Instance != null)
                UIManager.Instance.ShowHomeAfterSetup();

            // Show App Open Ad only after the first Subscription close.
            showOpenAdCoroutine = StartCoroutine(ShowOpenAdAfterClose());
        }

        // Restore banner after closing Subscription.
        showBannerAdCoroutine = StartCoroutine(ShowHideBannerAdDelay(true));
    }

    private void RefreshOnShow()
    {
        isPanelShown = true;

        // Nếu panel được mở lại khi coroutine quảng cáo đang chờ,
        // hủy quảng cáo để không hiển thị đè lên Subscription.
        StopAdCoroutines();

        if (isFirstShow)
        {
            selectedSubId = defaultToYearly ? "yearlyid" : "weeklyid";
        }

        ApplySelectedPack();
    }

    #endregion

    #region SUBSCRIPTION SELECTION

    public void ChooseSubPack(string subPackName)
    {
        if (string.IsNullOrWhiteSpace(subPackName)) return;

        switch (subPackName.Trim().ToLowerInvariant())
        {
            case "weekly":
            case "week":
            case "weeklyid":
                selectedSubId = "weeklyid";
                break;
            case "yearly":
            case "year":
            case "yearlyid":
                selectedSubId = "yearlyid";
                break;
            default:
                Debug.LogWarning("[SubPanel] Unknown subscription pack: " + subPackName);
                return;
        }

        ApplySelectedPack();
    }

    private void ApplySelectedPack()
    {
        bool isYearly = selectedSubId == "yearlyid";
        SetSubPackButtonState(isYearly ? 0 : 1);
        ShowHideYearlyTexts(isYearly);
        ShowHideWeeklyTexts(!isYearly);

        if (sub != null)
        {
            sub.productId = selectedSubId;
            sub.CheckProduct();
        }

        SetPriceText(selectedSubId);
    }

    // Assumed order: 0 = Yearly, 1 = Weekly.
    private void SetSubPackButtonState(int subCase)
    {
        if (subPackButtonImages == null) return;

        for (int i = 0; i < subPackButtonImages.Length; i++)
        {
            Image image = subPackButtonImages[i];
            if (image == null) continue;
            image.sprite = i == subCase ? pickingSubPackSprite : notPickingSubPackSprite;
        }
    }

    #endregion

    #region PRICE AND LANGUAGE TEXT

    private void SetPriceText(string subId)
    {
        if (priceText == null) return;

        string price = subId == "yearlyid"
            ? GetYearlySubMultiLanguagePriceText()
            : GetWeeklySubMultiLanguagePriceText();

        priceText.text = price;
    }

    private string GetYearlySubMultiLanguagePriceText()
    {
        return GetLocalizedPrice("yearlyid") + "/year";
    }

    private void ShowHideYearlyTexts(bool show)
    {
        if (yearlyTexts == null) return;
        foreach (Text item in yearlyTexts)
            if (item != null) item.gameObject.SetActive(show);
    }

    private string GetWeeklySubMultiLanguagePriceText()
    {
        return GetLocalizedPrice("weeklyid") + "/week";
    }

    private void ShowHideWeeklyTexts(bool show)
    {
        if (weeklyTexts == null) return;
        foreach (Text item in weeklyTexts)
            if (item != null) item.gameObject.SetActive(show);
    }

    private string GetLocalizedPrice(string productId)
    {
        if (IAPManager.Instance != null)
        {
            Product product = IAPManager.Instance.GetProduct(productId);
            if (product != null && product.metadata != null &&
                !string.IsNullOrEmpty(product.metadata.localizedPriceString))
                return product.metadata.localizedPriceString;
        }

        if (sub != null && sub.productId == productId)
            return "$" + sub.priceInUsd.ToString("0.##");

        return "--";
    }

    #endregion

    #region PURCHASE METHODS

    public void GetSubReward()
    {
        if (sub == null)
        {
            Debug.LogWarning("[SubPanel] IAPPuchase reference is missing.");
            return;
        }

        sub.productId = selectedSubId;
        sub.BuyButtonPress();
    }

    #endregion

    #region AD HOOKS

    private void HideBannerAd()
    {
        MaxAdManager.Instance?.HideBanner();
    }

    private IEnumerator ShowOpenAdAfterClose()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, adDelayAfterClose));

        showOpenAdCoroutine = null;

        if (isPanelShown)
            yield break;

        AppOpenAdManager appOpenAdManager = AppOpenAdManager.Instance;

        if (appOpenAdManager == null)
            yield break;

        appOpenAdManager.ShowAppOpenAd();
    }

    private IEnumerator ShowHideBannerAdDelay(bool show)
    {
        if (!show)
        {
            HideBannerAd();
            yield break;
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, adDelayAfterClose));

        showBannerAdCoroutine = null;

        if (isPanelShown)
            yield break;

        MaxAdManager.Instance?.ShowBanner();
    }

    private void StopAdCoroutines()
    {
        if (showOpenAdCoroutine != null)
        {
            StopCoroutine(showOpenAdCoroutine);
            showOpenAdCoroutine = null;
        }

        if (showBannerAdCoroutine != null)
        {
            StopCoroutine(showBannerAdCoroutine);
            showBannerAdCoroutine = null;
        }
    }

    #endregion
}
