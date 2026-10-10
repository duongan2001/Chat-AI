using System.Collections.Generic;
using UnityEngine;

public class MultiLanguageManager : MonoBehaviour
{
    #region VARIABLES

    public static MultiLanguageManager Instance;

    [Header("--- FIXED MULTI LANGUAGE TEXTS ---")]
    public List<FixedMultiLanguageText> fixedMultiLangagueTexts = new List<FixedMultiLanguageText>();

    private Language currentLanguage;

    #endregion


    #region UNITY METHODS

    private void Awake()
    {
        SetupInstance();

        if (Instance != this)
            return;

        string savedLanguage = PlayerPrefs.GetString(PlayerPrefKey.Language.ToString(), "English");

        currentLanguage = Language.En;

        foreach (Language language in System.Enum.GetValues(typeof(Language)))
        {
            if (GetLanguageEnglishName(language) == savedLanguage)
            {
                currentLanguage = language;
                break;
            }
        }
    }

    private void Start()
    {
        if (Instance != this)
            return;

        SetLanguageData(currentLanguage);
    }

    private void Update()
    {
        // No per-frame updates required.
    }

    #endregion


    #region SINGLETON METHODS

    private void SetupInstance()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }

        if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    #endregion


    #region LANGUAGE METHODS

    public void SetLanguageData(Language language)
    {
        if (!System.Enum.IsDefined(typeof(Language), language))
        {
            Debug.LogWarning($"[MultiLanguageManager] Invalid language: {language}");
            return;
        }

        currentLanguage = language;

        if (fixedMultiLangagueTexts == null)
        {
            fixedMultiLangagueTexts = new List<FixedMultiLanguageText>();
        }

        for (int i = fixedMultiLangagueTexts.Count - 1; i >= 0; i--)
        {
            FixedMultiLanguageText textItem = fixedMultiLangagueTexts[i];

            if (textItem == null)
            {
                fixedMultiLangagueTexts.RemoveAt(i);
                continue;
            }

            textItem.SetLanguage();
        }
    }

    public int GetCurrentLanguageId()
    {
        return (int)currentLanguage;
    }

    public void AddFixedTextItemToList(FixedMultiLanguageText fixedMultiLanguageText)
    {
        if (fixedMultiLanguageText == null)
            return;

        if (fixedMultiLangagueTexts == null)
        {
            fixedMultiLangagueTexts = new List<FixedMultiLanguageText>();
        }

        if (!fixedMultiLangagueTexts.Contains(fixedMultiLanguageText))
        {
            fixedMultiLangagueTexts.Add(fixedMultiLanguageText);
        }

        fixedMultiLanguageText.SetLanguage();
    }

    public void ChangeLanguage(int targetLanguageId)
    {
        if (!System.Enum.IsDefined(typeof(Language), targetLanguageId))
        {
            Debug.LogWarning($"[MultiLanguageManager] Invalid language ID: {targetLanguageId}");
            return;
        }

        Language targetLanguage = (Language)targetLanguageId;

        // Save the selected language using the existing
        // LanguageManager PlayerPrefs convention.
        PlayerPrefs.SetString(PlayerPrefKey.Language.ToString(), GetLanguageEnglishName(targetLanguage));
        PlayerPrefs.Save();

        SetLanguageData(targetLanguage);

        Debug.Log($"[MultiLanguageManager] Language changed to: {targetLanguage}");
    }

    public string GetCurrentLanguageString()
    {
        return GetLanguageEnglishName(currentLanguage);
    }

    #endregion


    #region LANGUAGE HELPER METHODS

    private string GetLanguageEnglishName(Language language)
    {
        switch (language)
        {
            case Language.En:
                return "English";

            case Language.Vi:
                return "Vietnamese";

            case Language.Th:
                return "Thai";

            case Language.Jp:
                return "Japanese";

            case Language.Kr:
                return "Korean";

            case Language.In:
                return "Hindi";

            case Language.Id:
                return "Indonesian";

            case Language.My:
                return "Burmese";

            case Language.Es:
                return "Spanish";

            case Language.Pt:
                return "Portuguese";

            case Language.Zh:
                return "Chinese";

            case Language.Ru:
                return "Russian";

            default:
                return "English";
        }
    }

    #endregion
}
