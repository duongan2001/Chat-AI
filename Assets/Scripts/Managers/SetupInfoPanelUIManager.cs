using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SetupInfoPanelUIManager : PanelBaseUIController
{
    #region VARIABLES

    [Header("--- SETUP PANELS ---")]
    [SerializeField] private PanelBaseUIController setupNameCanvasGroup;
    [SerializeField] private PanelBaseUIController helloCanvasGroup;
    [SerializeField] private PanelBaseUIController setupSexCanvasGroup;
    [SerializeField] private PanelBaseUIController setupDobCanvasGroup;
    [SerializeField] private PanelBaseUIController setupSexInterestCanvasGroup;
    [SerializeField] private PanelBaseUIController setupCategoryInterstgCanvasGroup;

    [Header("--- SETUP NAME ---")]
    [SerializeField] private InputField nameText;
    [SerializeField] private Text nameCharacterCountText;
    [SerializeField] private Button confirmNameButton;

    [Header("--- SETUP HELLO ---")]
    [SerializeField] private Text helloText;

    [Header("--- SETUP SEX ---")]
    [SerializeField] private Image[] sexButtons;
    [SerializeField] private Sprite choosingButtonSprite;
    [SerializeField] private Sprite normalButtonSprite;
    [SerializeField] private Button confirmSexButton;
    [SerializeField] private int currentSexId = -1;

    [Header("--- SETUP DOB ---")]
    [SerializeField] private Button confirmDobButton;

    [Header("--- SETUP SEX INTEREST ---")]
    [SerializeField] private Image[] sexInterestButtonBorders;
    [SerializeField] private Button confirmSexInterestButton;
    [SerializeField] private int currentSexInterestId = -1;

    [Header("--- SETUP CATEGORIES ---")]
    [SerializeField] private CategoryItem[] categoryItems;
    [SerializeField] private Button confirmCategoryButton;

    [Header("--- TRANSITION SETTINGS ---")]
    [SerializeField] private float helloDuration = 2f;
    [SerializeField] private float confirmButtonDelay = 2f;

    private const int MAX_NAME_LENGTH = 30;
    private const int MIN_CATEGORY_COUNT = 3;
    private int selectedCategoryCount = 0;
    private Coroutine helloCoroutine;
    private Coroutine dobButtonCoroutine;
    private string confirmedName = string.Empty;

    #endregion

    #region UNITY METHODS

    protected override void Awake()
    {
        base.Awake();
        // Serialized int fields default to 0 in Unity; -1 means no selection.
        // Explicitly reset on first setup to prevent a default selection.
        currentSexId = -1;
        currentSexInterestId = -1;
        
        if (nameText != null)
            nameText.characterLimit = MAX_NAME_LENGTH;
    }

    private void Start()
    {
        if (nameText != null)
            nameText.onValueChanged.AddListener(OnNameValueChanged);

        UpdateNameCharacterCount();
        CheckNameAvailable();
        RefreshSexButtons();
        RefreshSexInterestButtons();
        RefreshCategoryButton();
    }

    private void OnDestroy()
    {
        if (nameText != null)
            nameText.onValueChanged.RemoveListener(OnNameValueChanged);
    }

    private void OnDisable()
    {
        StopStepCoroutines();
    }

    #endregion

    #region PANEL METHODS

    public override void Show()
    {
        base.Show();
        ShowSetupName();
    }

    public override void ShowDelay()
    {
        base.ShowDelay();
        ShowSetupName();
    }

    public override void Hide()
    {
        StopStepCoroutines();
        SetStep(null);
        base.Hide();
    }

    private void SetStep(PanelBaseUIController activePanel)
    {
        // Each child panel controls its own CanvasGroup and fade transition.
        // Hide the previous panels, then show the selected panel.
        HideStep(setupNameCanvasGroup, activePanel);
        HideStep(helloCanvasGroup, activePanel);
        HideStep(setupSexCanvasGroup, activePanel);
        HideStep(setupDobCanvasGroup, activePanel);
        HideStep(setupSexInterestCanvasGroup, activePanel);
        HideStep(setupCategoryInterstgCanvasGroup, activePanel);

        if (activePanel != null)
            activePanel.Show();
    }

    private static void HideStep(PanelBaseUIController panel, PanelBaseUIController activePanel)
    {
        if (panel != null && panel != activePanel)
            panel.Hide();
    }

    private static void SetButtonVisible(Button button, bool visible)
    {
        if (button != null)
            button.gameObject.SetActive(visible);
    }

    private void StopStepCoroutines()
    {
        if (helloCoroutine != null)
        {
            StopCoroutine(helloCoroutine);
            helloCoroutine = null;
        }
        if (dobButtonCoroutine != null)
        {
            StopCoroutine(dobButtonCoroutine);
            dobButtonCoroutine = null;
        }
    }

    #endregion

    #region SETUP FLOW METHODS

    public void ShowSetupName()
    {
        StopStepCoroutines();
        SetStep(setupNameCanvasGroup);
        CheckNameAvailable();
    }

    public void BackToSetupName() 
    { 
        ShowSetupName();
    }

    public void ShowHello()
    {
        StopStepCoroutines();
        SetHelloText();
        SetStep(helloCanvasGroup);
        helloCoroutine = StartCoroutine(DelayHideHello());
    }

    private IEnumerator DelayHideHello()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, helloDuration));
        helloCoroutine = null;
        ShowSetupSex();
    }

    public void ShowSetupSex()
    {
        StopStepCoroutines();
        SetStep(setupSexCanvasGroup);
        RefreshSexButtons();
    }

    public void BackToSetupSex() { ShowSetupSex(); }

    public void ShowSetupDob()
    {
        StopStepCoroutines();
        SetStep(setupDobCanvasGroup);
        SetButtonVisible(confirmDobButton, false);
        if (confirmDobButton != null)
            dobButtonCoroutine = StartCoroutine(DelayShowButton(confirmDobButton));
    }

    public void BackToSetupDob() { ShowSetupDob(); }

    public void ShowSetupSexInterest()
    {
        StopStepCoroutines();
        SetStep(setupSexInterestCanvasGroup);
        RefreshSexInterestButtons();
    }

    public void BackToSetupSexInterest() { ShowSetupSexInterest(); }

    public void ShowSetupCategoryInterest()
    {
        StopStepCoroutines();
        SetStep(setupCategoryInterstgCanvasGroup);
        RefreshCategoryButton();
    }

    public void DoneSetup()
    {
        if (selectedCategoryCount < MIN_CATEGORY_COUNT)
            return;

        // Hook into your actual profile save/navigation manager here.
        Debug.Log("[SetupInfo] Setup completed. Profile persistence is not configured yet.");
        UIManager.Instance.CompleteSetupInfo();

        //UIManager.Instance.fakeLoadAiPanel.ShowDelay();
    }

    private IEnumerator DelayShowButton(Button button)
    {
        if (button == null) yield break;
        SetButtonVisible(button, false);
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, confirmButtonDelay));
        SetButtonVisible(button, true);
        dobButtonCoroutine = null;
    }

    #endregion

    #region NAME METHODS

    private void OnNameValueChanged(string value)
    {
        UpdateNameCharacterCount();
        CheckNameAvailable();
    }

    public void ConfirmName(string username)
    {
        string value = (username ?? string.Empty).Trim();
        if (value.Length == 0 || value.Length > MAX_NAME_LENGTH)
            return;

        confirmedName = value;
        ShowHello();
    }

    // For a no-argument Unity Button OnClick, use this wrapper.
    public void ConfirmCurrentName()
    {
        ConfirmName(nameText != null ? nameText.text : string.Empty);
    }

    public void CheckNameAvailable()
    {
        string value = nameText != null ? nameText.text.Trim() : string.Empty;
        SetButtonVisible(confirmNameButton, value.Length > 0 && value.Length <= MAX_NAME_LENGTH);
    }

    private void UpdateNameCharacterCount()
    {
        if (nameCharacterCountText == null) return;
        int length = nameText != null ? nameText.text.Length : 0;
        nameCharacterCountText.text = length + "/" + MAX_NAME_LENGTH;
    }

    private void SetHelloText()
    {
        if (helloText != null)
            helloText.text = "Hello, " + confirmedName + "!";
    }

    #endregion

    #region SEX METHODS

    public void ChooseSex(int id)
    {
        if (sexButtons == null || id < 0 || id >= sexButtons.Length)
            return;
        currentSexId = id;
        RefreshSexButtons();
    }

    private bool IsSexChose()
    {
        return sexButtons != null && currentSexId >= 0 && currentSexId < sexButtons.Length;
    }

    private void RefreshSexButtons()
    {
        if (sexButtons != null)
        {
            for (int i = 0; i < sexButtons.Length; i++)
            {
                if (sexButtons[i] == null) continue;
                Sprite sprite = i == currentSexId ? choosingButtonSprite : normalButtonSprite;
                if (sprite != null) sexButtons[i].sprite = sprite;
            }
        }
        SetButtonVisible(confirmSexButton, IsSexChose());
    }

    #endregion

    #region SEX INTEREST METHODS

    public void ChooseSexInterest(int id)
    {
        if (sexInterestButtonBorders == null || id < 0 || id >= sexInterestButtonBorders.Length)
            return;
        currentSexInterestId = id;
        RefreshSexInterestButtons();
    }

    private bool IsSexInterestChose()
    {
        return sexInterestButtonBorders != null &&
               currentSexInterestId >= 0 && currentSexInterestId < sexInterestButtonBorders.Length;
    }

    private void RefreshSexInterestButtons()
    {
        if (sexInterestButtonBorders != null)
        {
            for (int i = 0; i < sexInterestButtonBorders.Length; i++)
            {
                if (sexInterestButtonBorders[i] != null)
                    sexInterestButtonBorders[i].gameObject.SetActive(i == currentSexInterestId);
            }
        }
        SetButtonVisible(confirmSexInterestButton, IsSexInterestChose());
    }

    #endregion

    #region CATEGORY METHODS

    public void SelectCategory(int id)
    {
        if (categoryItems == null || id < 0 || id >= categoryItems.Length || categoryItems[id] == null)
            return;

        categoryItems[id].ChangeState();

        selectedCategoryCount = 0;

        for (int i = 0;i < categoryItems.Length;i++)
            if (categoryItems[i].isChosen)
                selectedCategoryCount++;

        RefreshCategoryButton();
        // CategoryItem's visual selection API is unknown; connect it here if needed.
    }

    private void RefreshCategoryButton()
    {
        SetButtonVisible(confirmCategoryButton, selectedCategoryCount >= MIN_CATEGORY_COUNT);
    }

    #endregion
}
