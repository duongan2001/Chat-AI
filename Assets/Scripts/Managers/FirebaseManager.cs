using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    #region VARIABLES

    public static FirebaseManager Instance;

    [Header("--- FIREBASE ---")]
    public bool isFirebaseReady;

    [Header("--- REMOTE CONFIG ---")]
    public bool canShowOpenAd = false;
    public bool canShowInterstitialAd = false;
    public float interstitialAdCooldown = 50f;
    public float interstitialAdCooldown_ing;

    private const string OPEN_AD_KEY = "canShowOpenAd";
    private const string INTERSTITIAL_AD_KEY = "canShowInterstitialAd";
    private const string INTERSTITIAL_COOLDOWN_KEY = "interstitialAdCooldown";

    private bool isRemoteConfigInitialized;
    private bool isFetchingRemoteConfig;

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

        InitializeFirebase();
    }

    private void Update()
    {
        if (interstitialAdCooldown_ing > 0f)
        {
            interstitialAdCooldown_ing = Mathf.Max(0f, interstitialAdCooldown_ing - Time.unscaledDeltaTime);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    #endregion

    #region FIREBASE INITIALIZATION

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (this == null || Instance != this)
                return;

            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("[Firebase] Dependency check failed: " + task.Exception);
                return;
            }

            if (task.Result != DependencyStatus.Available)
            {
                Debug.LogError("[Firebase] Dependencies unavailable: " + task.Result);
                return;
            }

            try
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                isFirebaseReady = true;
                Debug.Log("[Firebase] Initialized successfully.");
                InitializeRemoteConfig();
            }
            catch (Exception exception)
            {
                isFirebaseReady = false;
                Debug.LogError("[Firebase] Initialization failed: " + exception);
            }
        });
    }

    public void LogEvent(string eventName)
    {
        if (!isFirebaseReady || string.IsNullOrWhiteSpace(eventName))
            return;

        try
        {
            FirebaseAnalytics.LogEvent(eventName);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[Firebase] LogEvent failed: " + exception.Message);
        }
    }

    #endregion

    #region REMOTE CONFIG

    private void InitializeRemoteConfig()
    {
        if (!isFirebaseReady || isRemoteConfigInitialized)
            return;

        isRemoteConfigInitialized = true;

        var defaults = new Dictionary<string, object>
        {
            { OPEN_AD_KEY, canShowOpenAd },
            { INTERSTITIAL_AD_KEY, canShowInterstitialAd },
            { INTERSTITIAL_COOLDOWN_KEY, (double)Mathf.Max(0f, interstitialAdCooldown) }
        };

        FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults)
            .ContinueWithOnMainThread(task =>
            {
                if (this == null || Instance != this)
                    return;

                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogWarning("[Firebase] Remote Config defaults failed: " + task.Exception);
                    return;
                }

                ApplyRemoteConfigValues();
                FetchRemoteConfigData();
            });
    }

    private void FetchRemoteConfigData()
    {
        if (!isFirebaseReady || isFetchingRemoteConfig)
            return;

        isFetchingRemoteConfig = true;

        // In production, Firebase's minimum fetch interval applies unless configured otherwise.
        FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero)
            .ContinueWithOnMainThread(fetchTask =>
            {
                if (this == null || Instance != this)
                    return;

                if (fetchTask.IsCanceled || fetchTask.IsFaulted)
                {
                    isFetchingRemoteConfig = false;
                    Debug.LogWarning("[Firebase] Remote Config fetch failed: " + fetchTask.Exception);
                    return;
                }

                FirebaseRemoteConfig.DefaultInstance.ActivateAsync()
                    .ContinueWithOnMainThread(activateTask =>
                    {
                        if (this == null || Instance != this)
                            return;

                        isFetchingRemoteConfig = false;

                        if (activateTask.IsCanceled || activateTask.IsFaulted)
                        {
                            Debug.LogWarning("[Firebase] Remote Config activation failed: " + activateTask.Exception);
                            return;
                        }

                        ApplyRemoteConfigValues();
                        Debug.Log("[Firebase] Remote Config applied.");
                    });
            });
    }

    private void ApplyRemoteConfigValues()
    {
        if (!isFirebaseReady)
            return;

        FirebaseRemoteConfig config = FirebaseRemoteConfig.DefaultInstance;
        canShowOpenAd = config.GetValue(OPEN_AD_KEY).BooleanValue;
        canShowInterstitialAd = config.GetValue(INTERSTITIAL_AD_KEY).BooleanValue;
        interstitialAdCooldown = Mathf.Max(0f, (float)config.GetValue(INTERSTITIAL_COOLDOWN_KEY).DoubleValue);

        Debug.Log($"[Firebase] OpenAd={canShowOpenAd}, Interstitial={canShowInterstitialAd}, Cooldown={interstitialAdCooldown}s");
    }

    #endregion

    #region INTERSTITIAL COOLDOWN

    public void ResetInterstitialAdCooldown()
    {
        interstitialAdCooldown_ing = Mathf.Max(0f, interstitialAdCooldown);
    }

    public bool IsDoneCooldownInterstitialAd()
    {
        return interstitialAdCooldown_ing <= 0f;
    }

    #endregion
}
