using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    #region VARIABLES

    public static UIManager Instance;

    [Header("--- UI PANEL MANAGERS ---")]
    public CharacterProfilePanelUIManager characterProfilePanel;
    public ChatPanelUIManager chatPanel;
    public ChatboxPanelUIManager chatboxPanel;
    public DailyVipRewardUIManager dailyVipRewardPopup;
    public FakeLoadAIUIManager fakeLoadAiPanel;
    public HomePanelUIManager homePanel;
    public LanguagePanelUIManager languagePanel;
    public RewardPanelUIManager rewardPanel;
    public RoseNotiUIManager roseNotiPopup;
    public ShopPanelUIManager roseShopPanel;
    public SettingsPanelUIManager settingsPanel;
    public SetupInfoPanelUIManager setupInfoPanel;
    public SubPanelUIManager subPanel;
    public UnlockCharacterPanelUIManager unlockCharacterPopup;

    [Header("--- GENERAL BUTTONS ---")]
    [SerializeField] private Image[] generalButtonOnStateIcons;
    [SerializeField] private Image[] generalButtonOffStateIcons;

    [Header("--- ROSE AMOUNT ---")]
    [SerializeField] private Text[] roseAmountTexts;

    private const int HOME_BUTTON_ID = 0;
    private const int REWARD_BUTTON_ID = 1;
    private const int CHAT_BUTTON_ID = 2;
    private const int MISSION_BUTTON_ID = 3;

    private int currentGeneralButtonId = -1;

    private const string LANGUAGE_KEY = "Language";
    private const string SETUP_INFO_COMPLETED_KEY = "SetupInfoCompleted";

    private const float IAP_INIT_TIMEOUT = 15f;
    private Coroutine checkSubscriptionCoroutine;

    private bool hasCheckedVipRewardThisSession;

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
    }

    private void Awake()
    {
        SetupInstance();
    }

    private void Start()
    {
        UpdateRoseAmountUI();
        HideGeneralPanels();

        if (!PlayerPrefs.HasKey(LANGUAGE_KEY))
        {
            if (languagePanel != null)
            {
                languagePanel.Show();
                return;
            }
        }

        ContinueAfterLanguageSelection();
    }

    private void Update()
    {
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    #endregion


    #region ROSE METHODS

    public void UpdateRoseAmount(int amount)
    {
        string key = PlayerPrefKey.NumberOfRose.ToString();

        int currentRoses = PlayerPrefs.GetInt(key, 0);

        // Positive = add, Negative = subtract.
        long newAmount = (long)currentRoses + amount;

        // Rose amount cannot be negative or exceed int.MaxValue.
        int finalAmount = (int)System.Math.Max(0L, System.Math.Min(newAmount, (long)int.MaxValue));

        PlayerPrefs.SetInt(key, finalAmount);
        PlayerPrefs.Save();

        UpdateRoseAmountUI();
    }

    public void UpdateRoseAmountUI()
    {
        string key = PlayerPrefKey.NumberOfRose.ToString();

        int amount = PlayerPrefs.GetInt(key, 0);

        if (roseAmountTexts == null)
            return;

        for (int i = 0; i < roseAmountTexts.Length; i++)
        {
            if (roseAmountTexts[i] != null)
                roseAmountTexts[i].text = amount.ToString();
        }
    }

    #endregion


    #region GENERAL BUTTON METHODS

    public void OnGeneralHomeBottomButtonClick(string button)
    {
        if (string.IsNullOrWhiteSpace(button))
            return;

        switch (button.Trim().ToLowerInvariant())
        {
            case "home":
                OpenHomePanel();
                SetGeneralButtonStates(HOME_BUTTON_ID);
                break;

            case "chat":
            case "chatbox":
                OpenChatboxPanel();
                SetGeneralButtonStates(CHAT_BUTTON_ID);
                break;

            case "reward":
            case "rewards":
                OpenRewardPanel();
                SetGeneralButtonStates(REWARD_BUTTON_ID);
                break;

            case "mission":
                OpenMissionPanel();
                SetGeneralButtonStates(MISSION_BUTTON_ID);
                break;

            default:
                Debug.LogWarning("[UIManager] Unknown general button: " + button);
                break;
        }
    }

    private void SetGeneralButtonStates(int currentButtonId)
    {
        currentGeneralButtonId = currentButtonId;

        if (generalButtonOnStateIcons != null)
        {
            for (int i = 0; i < generalButtonOnStateIcons.Length; i++)
            {
                Image icon = generalButtonOnStateIcons[i];

                if (icon != null)
                    icon.gameObject.SetActive(i == currentButtonId);
            }
        }

        if (generalButtonOffStateIcons != null)
        {
            for (int i = 0; i < generalButtonOffStateIcons.Length; i++)
            {
                Image icon = generalButtonOffStateIcons[i];

                if (icon != null)
                    icon.gameObject.SetActive(i != currentButtonId);
            }
        }
    }

    #endregion


    #region PANEL NAVIGATION METHODS

    private void OpenHomePanel()
    {
        HideGeneralPanels();

        if (homePanel != null)
            homePanel.Show();

        CheckDailyVipReward();
    }

    private void OpenChatboxPanel()
    {
        HideGeneralPanels();

        if (chatboxPanel != null)
            chatboxPanel.Show();
    }

    private void OpenRewardPanel()
    {
        HideGeneralPanels();

        if (rewardPanel != null)
            rewardPanel.Show();
    }

    private void OpenMissionPanel()
    {
        HideGeneralPanels();

        //if (missionPanel != null)
            //missionPanel.Show();
    }

    private void HideGeneralPanels()
    {
        if (homePanel != null)
            homePanel.Hide();

        if (chatboxPanel != null)
            chatboxPanel.Hide();

        if (rewardPanel != null)
            rewardPanel.Hide();

        //if (missionPanel != null)
            //missionPanel.Hide();
    }

    #endregion


    #region SETUP INFO METHODS

    public void CompleteSetupInfo()
    {
        PlayerPrefs.SetInt(SETUP_INFO_COMPLETED_KEY, 1);
        PlayerPrefs.Save();

        if (setupInfoPanel != null)
            setupInfoPanel.Hide();

        HideGeneralPanels();

        if (fakeLoadAiPanel != null)
            fakeLoadAiPanel.Show();
        else
            OnFakeAILoadingCompleted();
    }

    public void ShowHomeAfterSetup()
    {
        OpenHomePanel();
        SetGeneralButtonStates(HOME_BUTTON_ID);
    }

    public void ContinueAfterLanguageSelection()
    {
        bool isSetupCompleted = PlayerPrefs.GetInt(SETUP_INFO_COMPLETED_KEY, 0) == 1;

        if (isSetupCompleted)
        {
            ShowHomeAfterSetup();
            return;
        }

        if (setupInfoPanel != null)
            setupInfoPanel.Show();
    }

    public void OnFakeAILoadingCompleted()
    {
        if (fakeLoadAiPanel != null)
            fakeLoadAiPanel.Hide();

        if (checkSubscriptionCoroutine != null)
            StopCoroutine(checkSubscriptionCoroutine);

        checkSubscriptionCoroutine = StartCoroutine(CheckSubscriptionAfterSetup());
    }

    private IEnumerator CheckSubscriptionAfterSetup()
    {
        float elapsed = 0f;

        while ((IAPManager.Instance == null ||
                !IAPManager.Instance.isInitialized) &&
               elapsed < IAP_INIT_TIMEOUT)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        checkSubscriptionCoroutine = null;

        if (IAPManager.Instance == null ||
            !IAPManager.Instance.isInitialized)
        {
            Debug.LogWarning("[UIManager] IAP initialization timeout.");
            ShowHomeAfterSetup();
            yield break;
        }

        bool hasSubscription =
            IAPManager.Instance.isSubscriptionPurchased;

        if (hasSubscription)
        {
            ShowHomeAfterSetup();
        }
        else if (subPanel != null)
        {
            subPanel.Show();
        }
        else
        {
            ShowHomeAfterSetup();
        }
    }

    #endregion


    #region VIP REWARD METHODS

    private void CheckDailyVipReward()
    {
        if (hasCheckedVipRewardThisSession)
            return;

        // Chưa có dữ liệu IAP thì chưa đánh dấu đã kiểm tra.
        if (IAPManager.Instance == null ||
            !IAPManager.Instance.isInitialized)
            return;

        hasCheckedVipRewardThisSession = true;

        if (dailyVipRewardPopup == null)
            return;

        if (dailyVipRewardPopup.CanClaimTodayReward())
            dailyVipRewardPopup.Show();
    }

    #endregion
}
