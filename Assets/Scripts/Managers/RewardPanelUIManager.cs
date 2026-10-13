using System;
using UnityEngine;
using UnityEngine.UI;

public class RewardPanelUIManager : PanelBaseUIController
{
    #region VARIABLES

    [Header("--- DAILY REWARD DATA ---")]
    public DailyRewardData data;

    private const string DAILY_REWARD_DATA_KEY = "DailyRewardData";

    [Header("--- AD REWARD SETTINGS ---")]
    [SerializeField] private int maxAdsPerDay = 5;
    [SerializeField] private int rosesPerAd = 10;

    [Header("--- DAILY REWARD ITEM LIST ---")]
    [SerializeField] private DailyRewardItem[] dailyRewardItems;

    [Header("--- DAILY REWARD BUTTONS ---")]
    [SerializeField] private Button checkInButton;
    [SerializeField] private Button watchAdButton;
    [SerializeField] private Text watchAdCount;

    #endregion

    #region UNITY METHODS

    protected override void Awake()
    {
        base.Awake();
        LoadDailyRewardData();
    }

    private void Start()
    {
        RefreshRewardUI();
    }

    #endregion

    #region PANEL METHODS

    public override void Show()
    {
        LoadDailyRewardData();
        base.Show();
        RefreshRewardUI();
    }

    public override void Hide()
    {
        base.Hide();
    }

    public override void ShowDelay()
    {
        LoadDailyRewardData();
        base.ShowDelay();
        RefreshRewardUI();
    }

    #endregion

    #region DATA METHODS

    public void SaveDailyRewardData()
    {
        if (data == null) return;
        PlayerPrefs.SetString(DAILY_REWARD_DATA_KEY, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public void LoadDailyRewardData()
    {
        string json = PlayerPrefs.GetString(DAILY_REWARD_DATA_KEY, string.Empty);
        if (!string.IsNullOrWhiteSpace(json))
        {
            try 
            { 
                data = JsonUtility.FromJson<DailyRewardData>(json); 
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[DailyReward] Invalid saved data: " + exception.Message);
                data = null;
            }
        }
        else
        {
            data = null;
        }

        if (data == null)
        {
            data = new DailyRewardData
            {
                lastRewardClaimedDate = string.Empty,
                lastRewardClaimedDay = 0,
                isTodayClaimed = false,
                adsWatchedToday = 0,
                lastActiveDate = string.Empty
            };
        }

        ResetDataIfDayPassed();
    }

    public void DeleteDailyRewardData()
    {
        PlayerPrefs.DeleteKey(DAILY_REWARD_DATA_KEY);
        PlayerPrefs.Save();
        
        data = new DailyRewardData
        {
            lastRewardClaimedDate = string.Empty,
            lastRewardClaimedDay = 0,
            isTodayClaimed = false,
            adsWatchedToday = 0,
            lastActiveDate = string.Empty
        };

        RefreshRewardUI();
    }

    private void ResetDataIfDayPassed()
    {
        if (data == null) return;

        string today = DateTime.Now.ToString("yyyy-MM-dd");
        bool changed = false;

        if (data.lastActiveDate != today)
        {
            data.lastActiveDate = today;
            data.adsWatchedToday = 0;
            changed = true;
        }

        // Start a fresh cycle on the day after the final reward was claimed.
        int rewardCount = dailyRewardItems == null ? 0 : dailyRewardItems.Length;

        if (rewardCount > 0 && data.lastRewardClaimedDay >= rewardCount &&
            data.lastRewardClaimedDate != today)
        {
            data.lastRewardClaimedDay = 0;
            changed = true;
        }

        bool claimedToday = data.lastRewardClaimedDate == today;

        if (data.isTodayClaimed != claimedToday)
        {
            data.isTodayClaimed = claimedToday;
            changed = true;
        }

        if (changed) 
            SaveDailyRewardData();
    }

    private bool IsDayPassed()
    {
        return data != null && data.lastRewardClaimedDate != DateTime.Now.ToString("yyyy-MM-dd");
    }

    #endregion

    #region DAILY REWARD METHODS

    // Returns a zero-based item index for the next claim.
    private int GetRewardDay()
    {
        int count = dailyRewardItems == null ? 0 : dailyRewardItems.Length;
        
        if (count <= 0 || data == null) 
            return -1;

        int claimedCount = Mathf.Max(0, data.lastRewardClaimedDay);
        
        return claimedCount % count;
    }

    // lastRewardDay is the zero-based item index to claim.
    private void ClaimReward(int lastRewardDay)
    {
        if (data == null || dailyRewardItems == null ||
            lastRewardDay < 0 || lastRewardDay >= dailyRewardItems.Length ||
            data.isTodayClaimed || !IsDayPassed())
            return;

        DailyRewardItem rewardItem = dailyRewardItems[lastRewardDay];
        if (rewardItem == null) return;

        int amount = Mathf.Max(0, rewardItem.Amount);
        AddRoses(amount);

        data.lastRewardClaimedDay = lastRewardDay + 1;
        data.lastRewardClaimedDate = DateTime.Now.ToString("yyyy-MM-dd");
        data.isTodayClaimed = true;
        SaveDailyRewardData();
        RefreshRewardUI();
    }

    public void ClaimTodayReward()
    {
        ResetDataIfDayPassed();

        if (data == null || data.isTodayClaimed || !IsDayPassed()) 
            return;

        ClaimReward(GetRewardDay());
    }

    private void SetRewardItemsState()
    {
        if (dailyRewardItems == null || data == null) 
            return;

        int nextIndex = GetRewardDay();
        int count = dailyRewardItems.Length;
        int claimedCount = Mathf.Clamp(data.lastRewardClaimedDay, 0, count);

        for (int i = 0; i < count; i++)
        {
            DailyRewardItem item = dailyRewardItems[i];
            
            if (item == null) 
                continue;

            bool claimed = i < claimedCount;
            bool isToday = !data.isTodayClaimed && i == nextIndex;
            item.SetBackgroundState(isToday);
            item.SetRoseIconState(claimed);
        }
    }

    public void SetTodayRewardItemClaimedState()
    {
        SetRewardItemsState();
    }

    #endregion

    #region AD REWARD METHODS

    public bool CanWatchAdForRose()
    {
        ResetDataIfDayPassed();
        
        return data != null && maxAdsPerDay > 0 &&
               data.adsWatchedToday < maxAdsPerDay;
    }

    public int GetRemainingAdViews()
    {
        ResetDataIfDayPassed();
        
        if (data == null) 
            return 0;

        return Mathf.Max(0, maxAdsPerDay - data.adsWatchedToday);
    }

    // Call ONLY from the successful rewarded-ad callback.
    public void ClaimRosesAd()
    {
        if (!CanWatchAdForRose()) 
            return;

        data.adsWatchedToday++;
        AddRoses(Mathf.Max(0, rosesPerAd));
        SaveDailyRewardData();
        UpdateButtonsState();
    }

    // Can be called when an ad becomes available to refresh the panel.
    public void ClaimRosesAllowedNoti()
    {
        UpdateButtonsState();
    }

    #endregion

    #region ROSE METHODS

    private void AddRoses(int amount)
    {
        if (amount <= 0) return;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateRoseAmount(amount);
        }
        else
        {
            string key = PlayerPrefKey.NumberOfRose.ToString();
            int current = PlayerPrefs.GetInt(key, 0);
            PlayerPrefs.SetInt(key, current > int.MaxValue - amount ? int.MaxValue : current + amount);
            PlayerPrefs.Save();
        }
    }

    #endregion

    #region UI METHODS

    private void RefreshRewardUI()
    {
        SetRewardItemsState();
        UpdateButtonsState();
    }

    private void UpdateButtonsState()
    {
        SetCheckInButtonState();
        SetWatchAdButtonState();
        SetWatchAdCountText();
    }

    private void SetCheckInButtonState()
    {
        if (checkInButton != null)
            checkInButton.interactable = data != null && !data.isTodayClaimed && GetRewardDay() >= 0;
    }

    private void SetWatchAdButtonState()
    {
        if (watchAdButton != null)
            watchAdButton.interactable = data != null && maxAdsPerDay > 0 && data.adsWatchedToday < maxAdsPerDay;
    }

    private void SetWatchAdCountText()
    {
        if (watchAdCount != null)
            watchAdCount.text = $"({maxAdsPerDay - GetRemainingAdViews()}/{Mathf.Max(0, maxAdsPerDay)})";
    }

    #endregion
}
