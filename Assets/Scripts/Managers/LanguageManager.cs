
using System;
using UnityEngine;

public class LanguageManager : MonoBehaviour
{
    #region VARIABLES

    private const string PREFS_KEY = "selected_language";

    public readonly (string DisplayName, string EnglishName)[] SupportedLanguages =
    {
        ("English", "English"),
        ("Tiếng Việt", "Vietnamese"),
        ("ภาษาไทย", "Thai"),
        ("日本語", "Japanese"),
        ("한국어", "Korean"),
        ("हिन्दी", "Hindi"),
        ("Bahasa Indonesia", "Indonesian"),
        ("မြန်မာဘာသာ", "Burmese"),
        ("Español", "Spanish"),
        ("Português", "Portuguese"),
        ("简体中文", "Chinese"),
        ("Русский", "Russian")
    };

    public Language CurrentLanguage { get; private set; } = Language.En;

    public event Action<Language> OnLanguageChanged;

    #endregion


    #region UNITY METHODS

    private void Start()
    {
        LoadLanguage();
    }

    #endregion


    #region LANGUAGE METHODS

    public string GetCurrentLanguageEnglishName()
    {
        int index = (int)CurrentLanguage;

        if (index >= 0 && index < SupportedLanguages.Length)
        {
            return SupportedLanguages[index].EnglishName;
        }

        return "English";
    }

    public void SetLanguage(string englishName)
    {
        if (string.IsNullOrWhiteSpace(englishName))
        {
            Debug.LogWarning("[LanguageManager] Language name is empty.");
            return;
        }

        for (int i = 0; i < SupportedLanguages.Length; i++)
        {
            if (string.Equals(SupportedLanguages[i].EnglishName, englishName, StringComparison.OrdinalIgnoreCase))
            {
                SetLanguageByEnum((Language)i);
                return;
            }
        }

        Debug.LogWarning($"[LanguageManager] Unsupported language: {englishName}");
    }

    private void SetLanguageByEnum(Language language)
    {
        CurrentLanguage = language;

        PlayerPrefs.SetString(PREFS_KEY, SupportedLanguages[(int)language].EnglishName);
        PlayerPrefs.Save();

        OnLanguageChanged?.Invoke(CurrentLanguage);
    }

    public bool HasSelectedLanguage()
    {
        return PlayerPrefs.HasKey(PREFS_KEY);
    }

    public static Language GetSavedLanguage()
    {
        string savedLanguage = PlayerPrefs.GetString("selected_language", "English");

        switch (savedLanguage.ToLowerInvariant())
        {
            case "english": return Language.En;
            case "vietnamese": return Language.Vi;
            case "thai": return Language.Th;
            case "japanese": return Language.Jp;
            case "korean": return Language.Kr;
            case "hindi": return Language.In;
            case "indonesian": return Language.Id;
            case "burmese": return Language.My;
            case "spanish": return Language.Es;
            case "portuguese": return Language.Pt;
            case "chinese": return Language.Zh;
            case "russian": return Language.Ru;
            default: return Language.En;
        }
    }

    #endregion


    #region SAVE & LOAD

    private void LoadLanguage()
    {
        if (!HasSelectedLanguage())
        {
            CurrentLanguage = Language.En;
            OnLanguageChanged?.Invoke(CurrentLanguage);
            return;
        }

        string savedLanguage = PlayerPrefs.GetString(PREFS_KEY, "English");

        for (int i = 0; i < SupportedLanguages.Length; i++)
        {
            if (string.Equals(SupportedLanguages[i].EnglishName, savedLanguage, StringComparison.OrdinalIgnoreCase))
            {
                CurrentLanguage = (Language)i;
                OnLanguageChanged?.Invoke(CurrentLanguage);
                return;
            }
        }

        CurrentLanguage = Language.En;
        OnLanguageChanged?.Invoke(CurrentLanguage);
    }

    #endregion
}
