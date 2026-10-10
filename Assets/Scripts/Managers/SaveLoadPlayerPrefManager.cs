using System;
using UnityEngine;

public class SaveLoadPlayerPrefManager : MonoBehaviour
{
    #region VARIABLES

    public static SaveLoadPlayerPrefManager Instance;

    private const int DEFAULT_DAILY_CHAT_REMAIN = 5;

    private const string DAILY_CHAT_REMAIN_KEY = "DailyChatRemain";
    private const string DAILY_CHAT_DATE_KEY = "DailyChatLastResetDate";

    #endregion


    #region UNITY METHODS

    private void Awake()
    {
        SetupInstance();
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);

        if (Instance != this)
            return;

        LoadDailyChatRemain();
    }

    #endregion


    #region SINGLETON METHODS

    private void SetupInstance()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            return;
        }

        if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    #endregion


    #region PLAYER PREF KEY METHODS

    private string GetKey(PlayerPrefKey key)
    {
        return key.ToString();
    }

    #endregion


    #region INT PLAYER PREF METHODS

    public int LoadIntPlayerPref(PlayerPrefKey key)
    {
        return PlayerPrefs.GetInt(GetKey(key), 0);
    }

    public void SaveIntPlayerPref(PlayerPrefKey key, int value)
    {
        PlayerPrefs.SetInt(GetKey(key), value);
        PlayerPrefs.Save();
    }

    #endregion


    #region FLOAT PLAYER PREF METHODS

    public float LoadFloatPlayerPref(PlayerPrefKey key)
    {
        return PlayerPrefs.GetFloat(GetKey(key), 0f);
    }

    public void SaveFloatPlayerPref(PlayerPrefKey key, float value)
    {
        PlayerPrefs.SetFloat(GetKey(key), value);
        PlayerPrefs.Save();
    }

    #endregion


    #region STRING PLAYER PREF METHODS

    public string LoadStringPlayerPref(PlayerPrefKey key)
    {
        return PlayerPrefs.GetString(GetKey(key), string.Empty);
    }

    public void SaveStringPlayerPref(PlayerPrefKey key, string value)
    {
        PlayerPrefs.SetString(GetKey(key), value ?? string.Empty);
        PlayerPrefs.Save();
    }

    #endregion


    #region DAILY CHAT METHODS

    private void LoadDailyChatRemain()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");

        string lastResetDate = PlayerPrefs.GetString(DAILY_CHAT_DATE_KEY, string.Empty);

        // First launch or a new calendar day.
        if (lastResetDate != today)
        {
            PlayerPrefs.SetInt(DAILY_CHAT_REMAIN_KEY, DEFAULT_DAILY_CHAT_REMAIN);
            PlayerPrefs.SetString(DAILY_CHAT_DATE_KEY, today);
            PlayerPrefs.Save();

            Debug.Log($"[SaveLoadPlayerPrefManager] Daily chat reset: {DEFAULT_DAILY_CHAT_REMAIN}");
        }
    }

    #endregion
}
