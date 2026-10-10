using UnityEngine;
using UnityEngine.UI;

public class FixedMultiLanguageText : MonoBehaviour
{
    #region VARIABLES

    [TextArea(5, 4)]
    [SerializeField]
    private string[] translatedValueList;

    [SerializeField]
    private Text targetText;

    #endregion


    #region UNITY METHODS

    private void Start()
    {
        if (MultiLanguageManager.Instance != null)
        {
            MultiLanguageManager.Instance.AddFixedTextItemToList(this);
        }
        else
        {
            SetLanguage();
        }
    }

    #endregion


    #region LANGUAGE METHODS

    public void SetLanguage()
    {
        if (targetText == null)
        {
            targetText = GetComponent<Text>();

            if (targetText == null)
            {
                Debug.LogWarning($"[FixedMultiLanguageText] Text component not found on {gameObject.name}");
                return;
            }
        }

        if (translatedValueList == null || translatedValueList.Length == 0)
        {
            return;
        }

        int languageIndex = MultiLanguageManager.Instance != null ? MultiLanguageManager.Instance.GetCurrentLanguageId() : (int)Language.En;

        // Use the selected language when available.
        if (languageIndex >= 0 &&
            languageIndex < translatedValueList.Length &&
            !string.IsNullOrWhiteSpace(translatedValueList[languageIndex]))
        {
            targetText.text = translatedValueList[languageIndex];
            return;
        }

        // Fallback to English.
        if (!string.IsNullOrWhiteSpace(translatedValueList[0]))
        {
            targetText.text = translatedValueList[0];
        }
    }

    #endregion
}
