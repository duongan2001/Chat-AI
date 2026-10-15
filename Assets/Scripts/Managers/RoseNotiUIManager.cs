using UnityEngine;
using UnityEngine.UI;

public class RoseNotiUIManager : PanelBaseUIController
{
    #region VARIABLES

    public static RoseNotiUIManager Instance;

    [Header("--- POPUPS ---")]
    [SerializeField] private RectTransform noMessagesAvailablePopup;
    [SerializeField] private RectTransform notEnoughRosesPopup;

    [Header("--- ROSE SETTINGS ---")]
    [SerializeField] private int roseCostPerMessage = 5;

    [Header("--- AD ---")]
    [SerializeField] private Button watchAdButton;
    [SerializeField] private Text watchAdCount;
    [SerializeField] private int maxAdsPerDay = 5;
    [SerializeField] private int rosesPerAd = 10;

    private bool isWatchingRewardedAd;

    [Header("--- ROSE EFFECT POINTS ---")]
    [SerializeField] private RectTransform adRewardRosePoint;
    [SerializeField] private RectTransform roseBalancePoint;

    #endregion


    #region UNITY METHODS

    protected override void Awake()
    {
        base.Awake();

        Instance = this;
    }

    private void Start()
    {
        HideAllPopups();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    #endregion


    #region POPUP METHODS

    private void HideAllPopups()
    {
        if (noMessagesAvailablePopup != null)
            noMessagesAvailablePopup.gameObject.SetActive(false);

        if (notEnoughRosesPopup != null)
            notEnoughRosesPopup.gameObject.SetActive(false);
    }

    public void ShowNoMessageAvailablePopup()
    {
        HideAllPopups();

        Show();

        if (noMessagesAvailablePopup != null)
            noMessagesAvailablePopup.gameObject.SetActive(true);
    }

    public void ShowNotEnoughRosesPopup()
    {
        HideAllPopups();

        Show();

        if (notEnoughRosesPopup != null)
            notEnoughRosesPopup.gameObject.SetActive(true);
    }

    #endregion


    #region ROSE METHODS

    public void AcceptUseRoseForMessage()
    {
        string roseKey = PlayerPrefKey.NumberOfRose.ToString();
        string chatKey = PlayerPrefKey.DailyChatRemain.ToString();

        int currentRoses = PlayerPrefs.GetInt(roseKey, 0);
        int cost = Mathf.Max(1, roseCostPerMessage);

        if (currentRoses < cost)
        {
            ShowNotEnoughRosesPopup();
            return;
        }

        // Consume Roses.
        PlayerPrefs.SetInt(roseKey, currentRoses - cost);

        // Grant one chat message.
        int remainingChats = PlayerPrefs.GetInt(chatKey, 0);
        PlayerPrefs.SetInt(chatKey, remainingChats + 1);

        PlayerPrefs.Save();

        // Update the Rose amount in the main UI.
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateRoseAmount(0);

        Hide();
    }

    #endregion


    #region SHOP METHODS

    public void GoToShop()
    {
        Hide();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.roseShopPanel.Show();
        }
        else
        {
            Debug.LogWarning("[RoseNoti] Shop Panel is not assigned.");
        }
    }

    #endregion


    #region PANEL METHODS

    public override void Show()
    {
        base.Show();
        RefreshWatchAdUI();
    }

    public override void ShowDelay()
    {
        base.ShowDelay();
        RefreshWatchAdUI();
    }

    public override void Hide()
    {
        HideAllPopups();
        base.Hide();
    }

    #endregion


    #region AD REWARD METHODS

    public bool CanWatchAdForRose()
    {
        if (SaveLoadPlayerPrefManager.Instance == null)
            return false;

        int watched = SaveLoadPlayerPrefManager.Instance.GetAdsWatchedToday();

        return maxAdsPerDay > 0 && watched < maxAdsPerDay;
    }

    public void WatchAdForRose()
    {
        if (isWatchingRewardedAd || !CanWatchAdForRose())
            return;

        if (MaxAdManager.Instance == null ||
            !MaxAdManager.Instance.IsRewardedReady())
        {
            Debug.LogWarning("[RoseNoti] Rewarded Ad is not ready.");
            return;
        }

        isWatchingRewardedAd = true;
        RefreshWatchAdUI();

        bool isShowing = MaxAdManager.Instance.ShowRewarded(
            onRewarded: () =>
            {
                ClaimRosesAd();
            },
            onClosed: () =>
            {
                isWatchingRewardedAd = false;
                RefreshWatchAdUI();
            }
        );

        if (!isShowing)
        {
            isWatchingRewardedAd = false;
            RefreshWatchAdUI();

            Debug.LogWarning("[RoseNoti] Failed to show Rewarded Ad.");
        }
    }

    private void ClaimRosesAd()
    {
        if (!CanWatchAdForRose())
            return;

        SaveLoadPlayerPrefManager.Instance.AddAdsWatchedToday();

        int amount = Mathf.Max(0, rosesPerAd);

        if (amount > 0)
        {
            if (UIManager.Instance != null)
            {
                if (adRewardRosePoint != null && roseBalancePoint != null)
                {
                    UIManager.Instance.UpdateRoseAmount(amount, adRewardRosePoint, roseBalancePoint);
                }
                else
                {
                    UIManager.Instance.UpdateRoseAmount(amount);
                }
            }
            else
            {
                string key = PlayerPrefKey.NumberOfRose.ToString();
                int current = PlayerPrefs.GetInt(key, 0);

                PlayerPrefs.SetInt(key, current > int.MaxValue - amount ? int.MaxValue : current + amount);
                PlayerPrefs.Save();
            }
        }

        RefreshWatchAdUI();
    }

    public void RefreshWatchAdUI()
    {
        if (SaveLoadPlayerPrefManager.Instance == null)
            return;

        int maxAds = Mathf.Max(0, maxAdsPerDay);
        int watched = SaveLoadPlayerPrefManager.Instance.GetAdsWatchedToday();

        if (watchAdButton != null)
        {
            watchAdButton.interactable = watched < maxAds && !isWatchingRewardedAd;
        }

        if (watchAdCount != null)
        {
            watchAdCount.text = $"Get More ({watched}/{maxAds})";
        }
    }

    #endregion
}