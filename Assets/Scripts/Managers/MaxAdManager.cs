using System;
using System.Collections;
using UnityEngine;
using Firebase.Analytics;
using static MaxSdkBase;

public class MaxAdManager : MonoBehaviour
{
    #region VARIABLES

    public static MaxAdManager Instance;

    [Header("--- MAX AD UNIT IDS ---")]
    [SerializeField] private string interstitialAdUnitId;
    [SerializeField] private string rewardedAdUnitId;
    [SerializeField] private string bannerAdUnitId;
    [SerializeField] private string openAdUnitId;

    [Header("--- INTERSTITIAL ---")]
    [SerializeField] private float interstitialRetryDelay = 2f;
    private int interstitialRetryAttempt;
    private float lastInterstitialShowTime = float.NegativeInfinity;

    [Header("--- REWARDED ---")]
    [SerializeField] private float rewardedRetryDelay = 2f;
    private int rewardedRetryAttempt;

    [Header("APP OPEN")]
    private bool isAppOpenShowing;
    private int appOpenRetryAttempt;

    [Header("--- INIT ---")]
    [SerializeField] private bool isInitialized;
    private Action rewardedCallback;
    private Action rewardedClosedCallback;

    private bool initializationRequested;
    private bool callbacksRegistered;
    private bool bannerCreated;
    private bool bannerWanted;
    private bool fullScreenAdShowing;
    private bool rewardedEarned;
    private bool rewardedPending;
    private bool isRewardedShowing;
    private Coroutine interstitialRetryCoroutine;
    private Coroutine rewardedRetryCoroutine;
    private Coroutine appOpenRetryCoroutine;

    public bool IsInitialized => isInitialized;

    #endregion

    #region UNITY METHODS

    private void SetupInstance()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Awake()
    {
        SetupInstance();
    }

    private void Start()
    {
        if (Instance == this)
            InitializeMax();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        if (callbacksRegistered)
            UnregisterCallbacks();

        Instance = null;
    }

    #endregion

    #region INITIALIZATION

    public void InitializeMax()
    {
        if (Instance != this || initializationRequested || isInitialized)
            return;

        initializationRequested = true;
        RegisterCallbacks();
        MaxSdkCallbacks.OnSdkInitializedEvent += OnMaxInitialized;
        MaxSdk.InitializeSdk();
    }

    private void OnMaxInitialized(MaxSdkBase.SdkConfiguration configuration)
    {
        if (isInitialized)
            return;

        isInitialized = true;
        Debug.Log("[MAX] SDK initialized.");

        LoadInterstitial();
        LoadRewarded();
        LoadAppOpen();
        ShowBanner();
    }

    private void RegisterCallbacks()
    {
        if (callbacksRegistered)
            return;

        callbacksRegistered = true;

        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoaded;
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailed;
        MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayed;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialDisplayFailed;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHidden;
        MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialClicked;

        MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedLoaded;
        MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedLoadFailed;
        MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedDisplayed;
        MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedDisplayFailed;
        MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedReceived;
        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedHidden;
        MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedClicked;

        MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoaded;
        MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdLoadFailed;
        MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClicked;
        MaxSdkCallbacks.Banner.OnAdExpandedEvent += OnBannerAdExpanded;
        MaxSdkCallbacks.Banner.OnAdCollapsedEvent += OnBannerAdCollapsed;

        MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += OnAppOpenLoaded;
        MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += OnAppOpenLoadFailed;
        MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += OnAppOpenDisplayed;
        MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += OnAppOpenDisplayFailed;
        MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAppOpenHidden;

        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnAdRevenuePaid;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaid;
        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnAdRevenuePaid;
        MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += OnAdRevenuePaid;
    }

    private void UnregisterCallbacks()
    {
        callbacksRegistered = false;
        MaxSdkCallbacks.OnSdkInitializedEvent -= OnMaxInitialized;

        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= OnInterstitialLoaded;
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= OnInterstitialLoadFailed;
        MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent -= OnInterstitialDisplayed;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= OnInterstitialDisplayFailed;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= OnInterstitialHidden;
        MaxSdkCallbacks.Interstitial.OnAdClickedEvent -= OnInterstitialClicked;

        MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= OnRewardedLoaded;
        MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= OnRewardedLoadFailed;
        MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent -= OnRewardedDisplayed;
        MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= OnRewardedDisplayFailed;
        MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= OnRewardedReceived;
        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= OnRewardedHidden;
        MaxSdkCallbacks.Rewarded.OnAdClickedEvent -= OnRewardedClicked;

        MaxSdkCallbacks.Banner.OnAdLoadedEvent -= OnBannerAdLoaded;
        MaxSdkCallbacks.Banner.OnAdLoadFailedEvent -= OnBannerAdLoadFailed;
        MaxSdkCallbacks.Banner.OnAdClickedEvent -= OnBannerAdClicked;
        MaxSdkCallbacks.Banner.OnAdExpandedEvent -= OnBannerAdExpanded;
        MaxSdkCallbacks.Banner.OnAdCollapsedEvent -= OnBannerAdCollapsed;

        MaxSdkCallbacks.AppOpen.OnAdLoadedEvent -= OnAppOpenLoaded;
        MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent -= OnAppOpenLoadFailed;
        MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent -= OnAppOpenDisplayed;
        MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent -= OnAppOpenDisplayFailed;
        MaxSdkCallbacks.AppOpen.OnAdHiddenEvent -= OnAppOpenHidden;

        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent -= OnAdRevenuePaid;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent -= OnAdRevenuePaid;
        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent -= OnAdRevenuePaid;
        MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent -= OnAdRevenuePaid;
    }

    #endregion

    #region INTERSTITIAL

    public void LoadInterstitial()
    {
        if (!isInitialized || string.IsNullOrEmpty(interstitialAdUnitId))
            return;

        MaxSdk.LoadInterstitial(interstitialAdUnitId);
    }

    public bool IsInterstitialReady()
    {
        return isInitialized && !string.IsNullOrEmpty(interstitialAdUnitId)
               && MaxSdk.IsInterstitialReady(interstitialAdUnitId);
    }

    public bool CanShowInterstitial()
    {
        if (!IsInterstitialReady() || fullScreenAdShowing || isAppOpenShowing)
            return false;

        if (!CanShowForcedAds())
            return false;

        FirebaseManager firebase = FirebaseManager.Instance;
        return firebase == null ||
               (firebase.canShowInterstitialAd && firebase.IsDoneCooldownInterstitialAd());
    }

    public bool ShowInterstitial(float cooldown = 0f)
    {
        if (!CanShowInterstitial())
            return false;

        if (Time.realtimeSinceStartup - lastInterstitialShowTime < Mathf.Max(0f, cooldown))
            return false;

        fullScreenAdShowing = true;
        MaxSdk.ShowInterstitial(interstitialAdUnitId);
        return true;
    }

    private void OnInterstitialLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        interstitialRetryAttempt = 0;
        StopRetry(ref interstitialRetryCoroutine);
    }

    private void OnInterstitialLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        Debug.LogWarning("[MAX] Interstitial load failed: " + errorInfo.Message);
        interstitialRetryAttempt++;
        ScheduleRetry(ref interstitialRetryCoroutine, interstitialRetryDelay, interstitialRetryAttempt, LoadInterstitial);
    }

    private void OnInterstitialDisplayed(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        if (adUnitId != interstitialAdUnitId) return;
        fullScreenAdShowing = true;
        lastInterstitialShowTime = Time.realtimeSinceStartup;
        FirebaseManager.Instance?.ResetInterstitialAdCooldown();
    }

    private void OnInterstitialDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        fullScreenAdShowing = false;
        Debug.LogWarning("[MAX] Interstitial display failed: " + errorInfo.Message);
        LoadInterstitial();
    }

    private void OnInterstitialHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        fullScreenAdShowing = false;
        LoadInterstitial();
    }

    private void OnInterstitialClicked(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

    #endregion

    #region REWARDED

    public void LoadRewarded()
    {
        if (!isInitialized || string.IsNullOrEmpty(rewardedAdUnitId))
            return;

        MaxSdk.LoadRewardedAd(rewardedAdUnitId);
    }

    public bool IsRewardedReady()
    {
        return isInitialized && !string.IsNullOrEmpty(rewardedAdUnitId)
               && MaxSdk.IsRewardedAdReady(rewardedAdUnitId);
    }

    public bool ShowRewarded(Action onRewarded = null, Action onClosed = null)
    {
        if (!IsRewardedReady() || fullScreenAdShowing || isAppOpenShowing)
            return false;

        rewardedCallback = onRewarded;
        rewardedClosedCallback = onClosed;
        rewardedEarned = false;
        rewardedPending = true;
        isRewardedShowing = true;
        fullScreenAdShowing = true;
        MaxSdk.ShowRewardedAd(rewardedAdUnitId);
        return true;
    }

    private void OnRewardedLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        rewardedRetryAttempt = 0;
        StopRetry(ref rewardedRetryCoroutine);
    }

    private void OnRewardedLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        Debug.LogWarning("[MAX] Rewarded load failed: " + errorInfo.Message);
        rewardedRetryAttempt++;
        ScheduleRetry(ref rewardedRetryCoroutine, rewardedRetryDelay, rewardedRetryAttempt, LoadRewarded);
    }

    private void OnRewardedDisplayed(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        fullScreenAdShowing = true;
    }

    private void OnRewardedDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        Debug.LogWarning("[MAX] Rewarded display failed: " + errorInfo.Message);
        FinishRewarded();
        LoadRewarded();
    }

    private void OnRewardedReceived(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
    {
        if (!rewardedPending || rewardedEarned)
            return;

        rewardedEarned = true;
        Action callback = rewardedCallback;
        rewardedCallback = null;
        callback?.Invoke();
    }

    private void OnRewardedHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        FinishRewarded();
        LoadRewarded();
    }

    private void FinishRewarded()
    {
        isRewardedShowing = false;
        fullScreenAdShowing = false;
        if (!rewardedPending)
            return;

        rewardedPending = false;
        rewardedCallback = null;
        Action callback = rewardedClosedCallback;
        rewardedClosedCallback = null;
        callback?.Invoke();
    }

    private void OnRewardedClicked(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

    #endregion

    #region BANNER

    private void InitializeBanner()
    {
        if (!isInitialized || bannerCreated || string.IsNullOrEmpty(bannerAdUnitId))
            return;

        var config = new AdViewConfiguration(AdViewPosition.BottomCenter);

        MaxSdk.CreateBanner(bannerAdUnitId, config);
        MaxSdk.SetBannerBackgroundColor(bannerAdUnitId, Color.clear);

        bannerCreated = true;
    }

    public void ShowBanner()
    {
        if (!CanShowForcedAds())
        {
            HideBanner();
            return;
        }

        bannerWanted = true;
        if (!isInitialized)
            return;

        InitializeBanner();
        if (bannerCreated)
            MaxSdk.ShowBanner(bannerAdUnitId);
    }

    public void HideBanner()
    {
        bannerWanted = false;
        if (bannerCreated)
            MaxSdk.HideBanner(bannerAdUnitId);
    }

    public void DestroyBanner()
    {
        bannerWanted = false;
        if (!bannerCreated)
            return;

        MaxSdk.DestroyBanner(bannerAdUnitId);
        bannerCreated = false;
    }

    private void OnBannerAdLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        if (adUnitId != bannerAdUnitId)
            return;

        Debug.Log("[MAX] Banner loaded.");

        if (bannerWanted && CanShowForcedAds())
        {
            MaxSdk.ShowBanner(bannerAdUnitId);
        }
        else
        {
            MaxSdk.HideBanner(bannerAdUnitId);
        }
    }

    private void OnBannerAdLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        Debug.LogWarning("[MAX] Banner load failed: " + errorInfo.Message);
    }

    private void OnBannerAdClicked(string adUnitId, MaxSdkBase.AdInfo adInfo) { }
    private void OnBannerAdExpanded(string adUnitId, MaxSdkBase.AdInfo adInfo) { }
    private void OnBannerAdCollapsed(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

    #endregion

    #region APP OPEN

    public void LoadAppOpen()
    {
        if (!isInitialized || string.IsNullOrEmpty(openAdUnitId))
            return;

        MaxSdk.LoadAppOpenAd(openAdUnitId);
    }

    public bool IsAppOpenReady()
    {
        return isInitialized && !string.IsNullOrEmpty(openAdUnitId)
               && MaxSdk.IsAppOpenAdReady(openAdUnitId);
    }

    public bool ShowAppOpenAd()
    {
        if (!IsAppOpenReady() || fullScreenAdShowing || isAppOpenShowing || !CanShowForcedAds())
            return false;

        FirebaseManager firebase = FirebaseManager.Instance;
        if (firebase != null && !firebase.canShowOpenAd)
            return false;

        isAppOpenShowing = true;
        fullScreenAdShowing = true;
        MaxSdk.ShowAppOpenAd(openAdUnitId);
        return true;
    }

    private void OnAppOpenLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        appOpenRetryAttempt = 0;
        StopRetry(ref appOpenRetryCoroutine);
    }

    private void OnAppOpenLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        Debug.LogWarning("[MAX] App Open load failed: " + errorInfo.Message);
        appOpenRetryAttempt++;
        ScheduleRetry(ref appOpenRetryCoroutine, 2f, appOpenRetryAttempt, LoadAppOpen);
    }

    private void OnAppOpenDisplayed(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        isAppOpenShowing = true;
        fullScreenAdShowing = true;
    }

    private void OnAppOpenDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        isAppOpenShowing = false;
        fullScreenAdShowing = false;
        Debug.LogWarning("[MAX] App Open display failed: " + errorInfo.Message);
        LoadAppOpen();
    }

    private void OnAppOpenHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        isAppOpenShowing = false;
        fullScreenAdShowing = false;
        LoadAppOpen();
    }

    #endregion

    #region AD PERMISSION

    private bool IsVipActive()
    {
        return IAPManager.Instance != null && IAPManager.Instance.isSubscriptionPurchased;
    }

    private bool CanShowForcedAds()
    {
        // Do not show forced ads until subscription ownership is known.
        return IAPManager.Instance != null && IAPManager.Instance.isInitialized && !IsVipActive();
    }

    #endregion

    #region RETRY AND REVENUE

    private void ScheduleRetry(ref Coroutine routine, float baseDelay, int attempt, Action loadAction)
    {
        StopRetry(ref routine);
        float delay = Mathf.Min(64f, Mathf.Max(1f, baseDelay) * Mathf.Pow(2f, Mathf.Min(attempt - 1, 6)));
        routine = StartCoroutine(RetryAfterDelay(delay, loadAction));
    }

    private IEnumerator RetryAfterDelay(float delay, Action loadAction)
    {
        yield return new WaitForSecondsRealtime(delay);
        loadAction?.Invoke();
    }

    private void StopRetry(ref Coroutine routine)
    {
        if (routine == null)
            return;
        StopCoroutine(routine);
        routine = null;
    }

    private void OnAdRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        if (adInfo == null) return;

        double revenue = adInfo.Revenue;
        Debug.Log($"[MAX] Revenue: ${revenue:F6}, network: {adInfo.NetworkName}, unit: {adUnitId}");

        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.isFirebaseReady)
            return;

        string adFormat = adInfo.AdFormat ?? "";
        try
        {
            FirebaseAnalytics.LogEvent(
                "ad_impression",
                new Parameter("ad_platform", "AppLovin"),
                new Parameter("ad_source", adInfo.NetworkName ?? ""),
                new Parameter("ad_unit_name", adUnitId ?? ""),
                new Parameter("ad_format", adFormat),
                new Parameter("value", revenue),
                new Parameter("currency", "USD")
            );
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[MAX] Firebase revenue logging failed: " + exception.Message);
        }
    }

    #endregion
}
