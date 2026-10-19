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
    public MissionPanelUIManager missionPanel;
    public PhotoViewPopupUIManager photoViewPopup;
    public RandomChatNotiPopupUIManager randomChatNotiPopup;
    public RewardPanelUIManager rewardPanel;
    public RoseNotiUIManager roseNotiPopup;
    public ShopPanelUIManager roseShopPanel;
    public SettingsPanelUIManager settingsPanel;
    public SetupInfoPanelUIManager setupInfoPanel;
    public SubPanelUIManager subPanel;
    public UnlockCharacterPanelUIManager unlockCharacterPopup;
    public UnlockPhotoPopupUIManager unlockPhotoPopup;

    [Header("--- GENERAL BUTTONS ---")]
    [SerializeField] private Image[] generalButtonOnStateIcons;
    [SerializeField] private Image[] generalButtonOffStateIcons;

    [Header("--- ROSE AMOUNT ---")]
    [SerializeField] private Text[] roseAmountTexts;

    [Header("--- ROSE BURST EFFECT ---")]
    [SerializeField] private RectTransform roseEffectContainer;
    [SerializeField] private Sprite roseEffectSprite;

    [SerializeField] private int roseParticleCount = 8;
    [SerializeField] private float roseParticleSize = 120f;
    [SerializeField] private float roseBurstRadius = 120f;
    [SerializeField] private float roseBurstDuration = 0.5f;
    [SerializeField] private float roseFlyDuration = 2f;

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

    public void UpdateRoseAmount(int amount, RectTransform startPoint, RectTransform endPoint)
    {
        UpdateRoseAmount(amount);

        if (amount > 0)
        {
            PlayRoseBurstEffect(startPoint, endPoint);
        }
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


    #region ROSE EFFECT METHODS

    public void PlayRoseBurstEffect(RectTransform startPoint, RectTransform endPoint)
    {
        if (roseEffectContainer == null ||
            roseEffectSprite == null ||
            startPoint == null ||
            endPoint == null)
        {
            Debug.LogWarning("[UIManager] Rose effect references are missing.");
            return;
        }

        Vector2 startPosition = GetRoseEffectLocalPosition(startPoint);
        Vector2 endPosition = GetRoseEffectLocalPosition(endPoint);

        int count = Mathf.Max(0, roseParticleCount);

        for (int i = 0; i < count; i++)
        {
            GameObject roseObject = new GameObject("Rose Particle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

            roseObject.transform.SetParent(roseEffectContainer, false);

            RectTransform rect = roseObject.GetComponent<RectTransform>();

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            rect.sizeDelta = Vector2.one * roseParticleSize;
            rect.anchoredPosition = startPosition;

            Image image = roseObject.GetComponent<Image>();

            image.sprite = roseEffectSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            StartCoroutine(AnimateRoseParticle(rect, image, startPosition, endPosition));
        }
    }

    private Vector2 GetRoseEffectLocalPosition(RectTransform target)
    {
        Canvas canvas = roseEffectContainer.GetComponentInParent<Canvas>();
        Canvas rootCanvas = canvas != null ? canvas.rootCanvas : null;

        Camera camera = null;

        if (rootCanvas != null &&
            rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            camera = rootCanvas.worldCamera;
        }

        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(camera, target.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(roseEffectContainer, screenPosition, camera, out Vector2 localPosition);

        return localPosition;
    }

    private IEnumerator AnimateRoseParticle(RectTransform rect, Image image, Vector2 startPosition, Vector2 endPosition)
    {
        float burstDuration = Mathf.Max(0.01f, roseBurstDuration);
        float flyDuration = Mathf.Max(0.01f, roseFlyDuration);

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Random.Range(roseBurstRadius * 0.5f, roseBurstRadius);

        Vector2 burstOffset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

        Vector2 burstPosition = startPosition + burstOffset;

        float randomRotation = Random.Range(-180f, 180f);

        rect.anchoredPosition = startPosition;
        rect.localScale = Vector3.zero;

        Color color = image.color;
        color.a = 1f;
        image.color = color;

        // PHASE 1: BURST + SCALE 0 -> 1
        float elapsed = 0f;

        while (elapsed < burstDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / burstDuration);

            // Burst movement: Ease Out Cubic.
            float moveT = 1f - Mathf.Pow(1f - t, 3f);

            rect.anchoredPosition = Vector2.Lerp(startPosition, burstPosition, moveT);

            // Scale: Ease Out Back, slightly overshoots 1.
            float c1 = 1.70158f;
            float c3 = c1 + 1f;

            float scaleT = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);

            rect.localScale = Vector3.one * scaleT;

            yield return null;
        }

        // Ensure burst is fully completed before flying.
        rect.anchoredPosition = burstPosition;
        rect.localScale = Vector3.one;

        // PHASE 2: FLY TO END POINT
        elapsed = 0f;

        while (elapsed < flyDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / flyDuration);

            // Smooth movement.
            float flyT = t * t * (3f - 2f * t);

            rect.anchoredPosition = Vector2.Lerp(burstPosition, endPosition, flyT);

            // Rotate while flying.
            rect.localRotation = Quaternion.Euler(0f, 0f, randomRotation * t);

            // Scale 1 -> 0.1.
            float scale = Mathf.Lerp(1f, 0.5f, t);
            rect.localScale = Vector3.one * scale;

            // Fade out near the destination.
            color = image.color;
            color.a = t < 0.75f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.75f) / 0.25f);

            image.color = color;

            yield return null;
        }

        if (rect != null)
            Destroy(rect.gameObject);
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

        if (missionPanel != null)
            missionPanel.Show();
    }

    private void HideGeneralPanels()
    {
        if (homePanel != null)
            homePanel.Hide();

        if (chatboxPanel != null)
            chatboxPanel.Hide();

        if (rewardPanel != null)
            rewardPanel.Hide();

        if (missionPanel != null)
            missionPanel.Hide();
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
            CheckSubscriptionOnStartup();
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

        bool hasSubscription = IAPManager.Instance.isSubscriptionPurchased;

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

    private void CheckSubscriptionOnStartup()
    {
        if (checkSubscriptionCoroutine != null)
        {
            StopCoroutine(checkSubscriptionCoroutine);
            checkSubscriptionCoroutine = null;
        }

        checkSubscriptionCoroutine = StartCoroutine(CheckSubscriptionAfterSetup());
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
