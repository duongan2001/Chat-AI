
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SettingsPanelUIManager : PanelBaseUIController
{
    #region VARIABLES

    [Header("--- URL DATA ---")]
    [SerializeField] private string urlJsonLink;
    [SerializeField] private UrlData UrlData;

    [Header("--- LANGUAGE TEXT ---")]
    [SerializeField] private Text currentLanguageText;

    private Coroutine getUrlCoroutine;

    private readonly string[] languageNames =
    {
        "English",
        "Tiếng Việt",
        "ไทย",
        "日本語",
        "한국어",
        "हिन्दी",
        "Bahasa Indonesia",
        "မြန်မာ",
        "Español",
        "Português",
        "中文",
        "Русский"
    };

    #endregion


    #region UNITY METHODS

    private void Start()
    {
        UpdateLanguageText();
        GetUrl();
    }

    private void OnDestroy()
    {
        if (getUrlCoroutine != null)
        {
            StopCoroutine(getUrlCoroutine);
            getUrlCoroutine = null;
        }
    }

    #endregion


    #region URL METHODS

    public void GetUrl()
    {
        if (string.IsNullOrWhiteSpace(urlJsonLink))
        {
            Debug.LogWarning("[Settings] URL JSON link is empty.");
            return;
        }

        if (getUrlCoroutine != null)
            StopCoroutine(getUrlCoroutine);

        getUrlCoroutine = StartCoroutine(GetUrlCoroutine());
    }

    private IEnumerator GetUrlCoroutine()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(urlJsonLink))
        {
            request.timeout = 15;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[Settings] Failed to load URL data: " + request.error);

                getUrlCoroutine = null;
                yield break;
            }

            string json = request.downloadHandler.text;

            try
            {
                UrlData downloadedData = JsonUtility.FromJson<UrlData>(json);

                if (downloadedData != null)
                {
                    UrlData = downloadedData;

                    Debug.Log("[Settings] URL data loaded successfully.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[Settings] Invalid URL JSON: " + ex.Message);
            }
        }

        getUrlCoroutine = null;
    }

    public void OpenUrl(string urlId)
    {
        if (UrlData == null)
        {
            Debug.LogWarning("[Settings] URL data is not loaded.");
            return;
        }

        string targetUrl = string.Empty;

        switch (urlId.Trim().ToLowerInvariant())
        {
            case "app":
            case "appurl":
                targetUrl = UrlData.appUrl;
                break;

            case "policy":
            case "policyurl":
                targetUrl = UrlData.policyUrl;
                break;

            case "managesub":
            case "managesuburl":
            case "subscription":
                targetUrl = UrlData.manageSubUrl;
                break;

            case "characterphoto":
            case "characterphotojsonurl":
                targetUrl = UrlData.characterPhotoJsonUrl;
                break;

            default:
                Debug.LogWarning("[Settings] Unknown URL ID: " + urlId);
                return;
        }

        if (string.IsNullOrWhiteSpace(targetUrl))
        {
            Debug.LogWarning("[Settings] Target URL is empty: " + urlId);
            return;
        }

        if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out Uri uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            Debug.LogWarning("[Settings] Invalid URL: " + targetUrl);
            return;
        }

        Application.OpenURL(targetUrl);
    }

    #endregion


    #region LANGUAGE METHODS

    public void UpdateLanguageText()
    {
        if (currentLanguageText == null)
            return;

        int languageIndex = 0;

        if (MultiLanguageManager.Instance != null)
        {
            languageIndex =
                MultiLanguageManager.Instance.GetCurrentLanguageId();
        }

        if (languageIndex < 0 ||
            languageIndex >= languageNames.Length)
        {
            languageIndex = 0;
        }

        currentLanguageText.text = languageNames[languageIndex];
    }

    #endregion


    #region PANEL METHODS

    public override void Show()
    {
        base.Show();
        UpdateLanguageText();
    }

    public override void ShowDelay()
    {
        base.ShowDelay();
        UpdateLanguageText();
    }

    public override void Hide()
    {
        base.Hide();
    }

    #endregion
}
