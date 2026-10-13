using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        SetGeneralButtonStates(HOME_BUTTON_ID);
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
}
