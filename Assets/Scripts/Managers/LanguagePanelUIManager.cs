using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LanguagePanelUIManager : PanelBaseUIController
{
    #region VARIABLES

    [Header("--- NEXT BUTTON ---")]
    [SerializeField] private Button nextButton;
    [SerializeField] private int choosingLanguage = -1;

    private Coroutine delayShowNextButton;

    [Header("--- LIST TOGGLE ---")]
    [SerializeField] private Image[] toggles;
    [SerializeField] private Sprite toggleOn;
    [SerializeField] private Sprite toggleOff;

    [Header("--- SETTINGS ---")]
    [SerializeField] private float nextButtonDelay = 0.5f;

    private const string LANGUAGE_KEY = "Language";

    #endregion


    #region UNITY METHODS

    private void Awake()
    {
        // Không gọi base.Awake() nếu PanelBaseUIController
        // không khai báo Awake là virtual.
    }

    private void Start()
    {
        int savedLanguage = PlayerPrefs.GetInt(LANGUAGE_KEY, 0);

        choosingLanguage = savedLanguage;

        UpdateLanguageButtonToggles(choosingLanguage);

        if (nextButton != null)
            nextButton.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        StopNextButtonCoroutine();
    }

    #endregion


    #region PANEL METHODS

    public override void Show()
    {
        base.Show();

        LoadCurrentLanguage();
        ShowNextButtonDelay();
    }

    public override void Hide()
    {
        StopNextButtonCoroutine();

        base.Hide();
    }

    public override void ShowDelay()
    {
        base.ShowDelay();

        LoadCurrentLanguage();
        ShowNextButtonDelay();
    }

    #endregion


    #region NEXT BUTTON METHODS

    private void ShowNextButtonDelay()
    {
        StopNextButtonCoroutine();

        if (nextButton == null)
            return;

        nextButton.gameObject.SetActive(false);

        if (choosingLanguage < 0)
            return;

        delayShowNextButton = StartCoroutine(DelayShowNextButton());
    }

    private IEnumerator DelayShowNextButton()
    {
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, nextButtonDelay)
        );

        if (nextButton != null)
            nextButton.gameObject.SetActive(true);

        delayShowNextButton = null;
    }

    private void StopNextButtonCoroutine()
    {
        if (delayShowNextButton != null)
        {
            StopCoroutine(delayShowNextButton);
            delayShowNextButton = null;
        }
    }

    #endregion


    #region LANGUAGE METHODS

    private void LoadCurrentLanguage()
    {
        choosingLanguage = PlayerPrefs.GetInt(LANGUAGE_KEY, 0);

        UpdateLanguageButtonToggles(choosingLanguage);
    }

    public void ChooseLanguage()
    {
        if (choosingLanguage < 0)
            return;

        PlayerPrefs.SetInt(LANGUAGE_KEY, choosingLanguage);
        PlayerPrefs.Save();

        if (UIManager.Instance != null &&
            UIManager.Instance.settingsPanel != null)
        {
            UIManager.Instance.settingsPanel.UpdateLanguageText();
        }

        Hide();

        if (UIManager.Instance != null &&
            PlayerPrefs.GetInt("SetupInfoCompleted", 0) == 0)
        {
            UIManager.Instance.ContinueAfterLanguageSelection();
        }
    }

    public void UpdateLanguageButtonToggles(int id)
    {
        if (toggles == null || id < 0 || id >= toggles.Length)
            return;

        choosingLanguage = id;

        for (int i = 0; i < toggles.Length; i++)
        {
            if (toggles[i] == null)
                continue;

            toggles[i].sprite = i == choosingLanguage ? toggleOn : toggleOff;
        }

        ShowNextButtonDelay();
    }

    #endregion
}
