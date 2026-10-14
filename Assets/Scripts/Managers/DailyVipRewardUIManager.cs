using UnityEngine;
using UnityEngine.UI;

public class DailyVipRewardUIManager : PanelBaseUIController
{
    #region VARIABLES

    [SerializeField] private Text amountText;

    [Header("--- VIP DAILY REWARD ---")]
    [SerializeField] private int weeklyRoseAmount = 100;
    [SerializeField] private int yearlyRoseAmount = 150;

    private const string LAST_VIP_CLAIM_DATE_KEY = "LastVipRewardClaimedDate";

    private int currentRewardAmount;
    private bool isRewardAvailable;

    #endregion


    #region UNITY METHODS

    private void Start()
    {
        SetRewardText();
    }

    #endregion


    #region PANEL METHODS

    public override void Show()
    {
        RefreshReward();
        base.Show();
    }

    public override void ShowDelay()
    {
        RefreshReward();
        base.ShowDelay();
    }

    public override void Hide()
    {
        base.Hide();
    }

    #endregion


    #region VIP REWARD METHODS

    public bool CanClaimTodayReward()
    {
        if (IAPManager.Instance == null ||
            !IAPManager.Instance.isInitialized)
            return false;

        if (!IAPManager.Instance.isSubscriptionPurchased)
            return false;

        string today = System.DateTime.Now.ToString("yyyy-MM-dd");

        string lastClaimDate = PlayerPrefs.GetString(LAST_VIP_CLAIM_DATE_KEY, "");

        return lastClaimDate != today;
    }

    private int GetVipDailyRoseAmount()
    {
        if (IAPManager.Instance == null)
            return 0;

        int totalReward = 0;

        if (IAPManager.Instance.CheckHasSub("yearlyid"))
            totalReward += yearlyRoseAmount;

        if (IAPManager.Instance.CheckHasSub("weeklyid"))
            totalReward += weeklyRoseAmount;

        return totalReward;
    }

    private void RefreshReward()
    {
        currentRewardAmount = GetVipDailyRoseAmount();

        isRewardAvailable = currentRewardAmount > 0 && CanClaimTodayReward();

        SetRewardText();
    }

    public void ClaimReward()
    {
        RefreshReward();

        if (!isRewardAvailable)
        {
            Hide();
            return;
        }

        string today = System.DateTime.Now.ToString("yyyy-MM-dd");

        // Lưu trạng thái nhận thưởng trước khi cộng Rose.
        PlayerPrefs.SetString(LAST_VIP_CLAIM_DATE_KEY, today);
        PlayerPrefs.Save();

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateRoseAmount(currentRewardAmount);

        isRewardAvailable = false;

        Hide();
    }

    private void SetRewardText()
    {
        if (amountText == null)
            return;

        amountText.text = "+" + currentRewardAmount;
    }

    #endregion
}