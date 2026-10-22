using System;
using System.Collections;
using Firebase.Analytics;
using Firebase.Crashlytics;
using GoogleMobileAds.Api;
using UnityEngine;

public class AppOpenAdManager : MonoBehaviour
{
    #region VARIABLES

    public static AppOpenAdManager Instance;

#if UNITY_ANDROID
    private const string AD_UNIT_ID = "ca-app-pub-9674055550946724/4602392238";
#elif UNITY_IOS
    private const string AD_UNIT_ID = "ca-app-pub-5745357971752287/5920025684";
#else
    private const string AD_UNIT_ID = "unexpected_platform";
#endif

    private const float AD_EXPIRATION_HOURS = 4f;
    private const float RETRY_DELAY = 3f;

    private AppOpenAd appOpenAd;
    private DateTime loadTime;

    private bool isLoaded;
    private bool isLoading;

    public bool isShowing;

    private bool hasEnteredBackground;

    private Coroutine retryLoadAdCoroutine;

    public bool IsAdAvailable =>
        appOpenAd != null &&
        isLoaded &&
        (DateTime.UtcNow - loadTime).TotalHours < AD_EXPIRATION_HOURS;

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
        if (Instance != this)
            return;

        LoadAppOpenAd();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        StopRetryLoad();
        DestroyCurrentAd();

        Instance = null;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (Instance != this)
            return;

        if (pauseStatus)
        {
            hasEnteredBackground = true;
            return;
        }

        if (!hasEnteredBackground)
            return;

        hasEnteredBackground = false;

        ShowAppOpenAd();
    }

    #endregion


    #region LOAD APP OPEN AD

    public void LoadAppOpenAd()
    {
        if (isLoading || isShowing)
            return;

        StopRetryLoad();
        DestroyCurrentAd();

        isLoading = true;

        AppOpenAd.Load(AD_UNIT_ID, new AdRequest(), (ad, error) =>
        {
            if (this == null)
            {
                ad?.Destroy();
                return;
            }

            isLoading = false;

            if (error != null || ad == null)
            {
                Debug.LogWarning("[AppOpenAd] Load failed: " + error);
                ScheduleRetryLoad();
                return;
            }

            appOpenAd = ad;
            isLoaded = true;
            loadTime = DateTime.UtcNow;

            RegisterEventHandlers(ad);

            Debug.Log("[AppOpenAd] Loaded successfully.");
        });
    }

    private void ScheduleRetryLoad()
    {
        if (retryLoadAdCoroutine != null)
            return;

        retryLoadAdCoroutine = StartCoroutine(RetryLoadRoutine());
    }

    private IEnumerator RetryLoadRoutine()
    {
        yield return new WaitForSecondsRealtime(RETRY_DELAY);

        retryLoadAdCoroutine = null;

        LoadAppOpenAd();
    }

    private void StopRetryLoad()
    {
        if (retryLoadAdCoroutine == null)
            return;

        StopCoroutine(retryLoadAdCoroutine);
        retryLoadAdCoroutine = null;
    }

    private void DestroyCurrentAd()
    {
        if (appOpenAd != null)
        {
            appOpenAd.Destroy();
            appOpenAd = null;
        }

        isLoaded = false;
    }

    #endregion


    #region SHOW APP OPEN AD

    public bool CanShowOpenAd()
    {
        // Disable App Open Ad for version 1.0.1.
        if (Application.version == "1.0.1")
            return false;

        FirebaseManager firebase = FirebaseManager.Instance;

        if (firebase == null ||
            !firebase.isFirebaseReady ||
            !firebase.canShowOpenAd)
            return false;

        // Do not show ads for VIP users.
        IAPManager iap = IAPManager.Instance;

        if (iap == null || !iap.isInitialized || iap.isSubscriptionPurchased)
            return false;

        return true;
    }

    public void ShowAppOpenAd()
    {
        if (!CanShowOpenAd() || isShowing)
            return;

        if (!IsAdAvailable)
        {
            if (!isLoading)
                LoadAppOpenAd();

            return;
        }

        if (appOpenAd == null || !appOpenAd.CanShowAd())
        {
            Debug.LogWarning("[AppOpenAd] Ad is not ready.");

            LoadAppOpenAd();
            return;
        }

        isShowing = true;

        Debug.Log("[AppOpenAd] Showing ad.");

        try
        {
            appOpenAd.Show();

            FirebaseManager.Instance?.LogEvent("Show_Openads");
        }
        catch (Exception exception)
        {
            isShowing = false;

            Debug.LogError("[AppOpenAd] Show failed: " + exception);

            LoadAppOpenAd();
        }
    }

    #endregion


    #region AD CALLBACKS

    private void RegisterEventHandlers(AppOpenAd ad)
    {
        ad.OnAdPaid += OnAdPaid;

        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("[AppOpenAd] Impression recorded.");
        };

        ad.OnAdClicked += () =>
        {
            Debug.Log("[AppOpenAd] Clicked.");
        };

        ad.OnAdFullScreenContentOpened += () =>
        {
            isShowing = true;

            Debug.Log("[AppOpenAd] Opened.");
        };

        ad.OnAdFullScreenContentClosed += () =>
        {
            isShowing = false;

            Debug.Log("[AppOpenAd] Closed.");

            LoadAppOpenAd();
        };

        ad.OnAdFullScreenContentFailed += error =>
        {
            isShowing = false;

            Debug.LogWarning("[AppOpenAd] Display failed: " + error);

            LoadAppOpenAd();
        };
    }

    #endregion


    #region AD REVENUE

    private void OnAdPaid(AdValue adValue)
    {
        if (adValue == null)
            return;

        try
        {
            double revenue = adValue.Value / 1000000.0;

            Parameter[] parameters =
            {
                new Parameter("ad_platform", "admob"),
                new Parameter("ad_format", "app_open"),
                new Parameter("currency", adValue.CurrencyCode),
                new Parameter("value", revenue),
                new Parameter("ad_unit_name", AD_UNIT_ID),
                new Parameter("ad_unit_id", AD_UNIT_ID),
                new Parameter("ad_source", "admob"),
                new Parameter("placement", "open_game"),
                new Parameter("revenue_precision", adValue.Precision.ToString())
            };

            FirebaseManager firebase = FirebaseManager.Instance;

            if (firebase != null && firebase.isFirebaseReady)
            {
                FirebaseAnalytics.LogEvent("ad_impression", parameters);
            }

            Debug.Log($"[AppOpenAd] Revenue: {revenue} {adValue.CurrencyCode}");
        }
        catch (Exception exception)
        {
            Debug.LogError("[AppOpenAd] Revenue tracking failed: " + exception);

            if (FirebaseManager.Instance != null &&
                FirebaseManager.Instance.isFirebaseReady)
            {
                Crashlytics.LogException(exception);
            }
        }
    }

    #endregion
}
