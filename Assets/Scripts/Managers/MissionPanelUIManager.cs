using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MissionPanelUIManager : PanelBaseUIController
{
    #region VARIABLES

    public static MissionPanelUIManager Instance;

    [Serializable]
    public class MissionUI
    {
        [Header("--- MISSION SETTINGS ---")]
        public string missionName;
        public int initialTarget = 10;
        public int targetIncrease = 10;
        public int roseReward = 5;

        [Header("--- UI ---")]
        public Text progressText;
        public Text rewardText;
        public Button claimButton;
        public Text claimButtonText;
    }

    [Header("--- MISSIONS ---")]
    [SerializeField] private MissionUI sendMessageMission;
    [SerializeField] private MissionUI viewProfileMission;
    [SerializeField] private MissionUI chatCharacterMission;

    private const string MISSION_DATE_KEY = "Mission_LastDate";
    private const string MESSAGE_PROGRESS_KEY = "Mission_MessageProgress";
    private const string PROFILE_PROGRESS_KEY = "Mission_ProfileProgress";
    private const string CHAT_PROGRESS_KEY = "Mission_ChatProgress";

    private const string MESSAGE_LEVEL_KEY = "Mission_MessageLevel";
    private const string PROFILE_LEVEL_KEY = "Mission_ProfileLevel";
    private const string CHAT_LEVEL_KEY = "Mission_ChatLevel";

    private const string VIEWED_PROFILES_KEY = "Mission_ViewedProfiles";
    private const string CHATTED_CHARACTERS_KEY = "Mission_ChattedCharacters";

    private HashSet<string> viewedProfiles = new HashSet<string>();
    private HashSet<string> chattedCharacters = new HashSet<string>();

    #endregion


    #region UNITY METHODS

    protected override void Awake()
    {
        base.Awake();
        Instance = this;

        LoadMissionData();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    #endregion


    #region PANEL METHODS

    public override void Show()
    {
        base.Show();

        CheckDailyReset();
        RefreshMissionUI();
    }

    public override void ShowDelay()
    {
        base.ShowDelay();

        CheckDailyReset();
        RefreshMissionUI();
    }

    #endregion


    #region MISSION PROGRESS METHODS

    // Call after the user successfully sends a message.
    public void AddMessageProgress()
    {
        CheckDailyReset();
        AddProgress(MESSAGE_PROGRESS_KEY);
    }

    // Call when the user opens a character profile.
    public void AddProfileProgress(string characterName)
    {
        CheckDailyReset();

        if (string.IsNullOrEmpty(characterName))
            return;

        if (!viewedProfiles.Add(characterName))
            return;

        SaveStringSet(VIEWED_PROFILES_KEY, viewedProfiles);
        AddProgress(PROFILE_PROGRESS_KEY);
    }

    // Call after the user sends a message to a character.
    public void AddChatCharacterProgress(string characterName)
    {
        CheckDailyReset();

        if (string.IsNullOrEmpty(characterName))
            return;

        if (!chattedCharacters.Add(characterName))
            return;

        SaveStringSet(CHATTED_CHARACTERS_KEY, chattedCharacters);
        AddProgress(CHAT_PROGRESS_KEY);
    }

    private void AddProgress(string progressKey)
    {
        int current = PlayerPrefs.GetInt(progressKey, 0);

        PlayerPrefs.SetInt(progressKey, current == int.MaxValue ? current : current + 1);
        PlayerPrefs.Save();

        RefreshMissionUI();
    }

    #endregion


    #region CLAIM METHODS

    public void ClaimMessageMission()
    {
        ClaimMission(sendMessageMission, MESSAGE_PROGRESS_KEY, MESSAGE_LEVEL_KEY);
    }

    public void ClaimProfileMission()
    {
        ClaimMission(viewProfileMission, PROFILE_PROGRESS_KEY, PROFILE_LEVEL_KEY);
    }

    public void ClaimChatCharacterMission()
    {
        ClaimMission(chatCharacterMission, CHAT_PROGRESS_KEY, CHAT_LEVEL_KEY);
    }

    private void ClaimMission(MissionUI mission, string progressKey, string levelKey)
    {
        CheckDailyReset();

        if (mission == null)
            return;

        int progress = PlayerPrefs.GetInt(progressKey, 0);
        int level = PlayerPrefs.GetInt(levelKey, 0);
        int target = GetMissionTarget(mission, level);

        // Not enough progress.
        if (progress < target)
            return;

        // Increase mission target.
        PlayerPrefs.SetInt(levelKey, level == int.MaxValue ? level : level + 1);
        PlayerPrefs.Save();

        // Give Roses.
        int reward = Mathf.Max(0, mission.roseReward);

        if (reward > 0)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateRoseAmount(reward);
            }
            else
            {
                string roseKey = PlayerPrefKey.NumberOfRose.ToString();

                int currentRoses = PlayerPrefs.GetInt(roseKey, 0);

                long total = (long)currentRoses + reward;

                PlayerPrefs.SetInt(roseKey, (int)Math.Min(total, int.MaxValue));
                PlayerPrefs.Save();
            }
        }

        RefreshMissionUI();
    }

    #endregion


    #region MISSION UI METHODS

    public void RefreshMissionUI()
    {
        RefreshMissionItem(sendMessageMission, MESSAGE_PROGRESS_KEY, MESSAGE_LEVEL_KEY);
        RefreshMissionItem(viewProfileMission, PROFILE_PROGRESS_KEY, PROFILE_LEVEL_KEY);
        RefreshMissionItem(chatCharacterMission, CHAT_PROGRESS_KEY, CHAT_LEVEL_KEY);
    }

    private void RefreshMissionItem(MissionUI mission, string progressKey, string levelKey)
    {
        if (mission == null)
            return;

        int progress = PlayerPrefs.GetInt(progressKey, 0);
        int level = PlayerPrefs.GetInt(levelKey, 0);
        int target = GetMissionTarget(mission, level);
        
        bool canClaim = progress >= target;

        if (mission.progressText != null)
        {
            string description = "";

            if (progressKey == MESSAGE_PROGRESS_KEY)
                description = $"Send {target} messages today";
            else if (progressKey == PROFILE_PROGRESS_KEY)
                description = $"View {target} character profiles";
            else if (progressKey == CHAT_PROGRESS_KEY)
                description = $"Chat with {target} different characters";

            mission.progressText.text = $"{description} ({Mathf.Min(progress, target)}/{target})";
        }

        if (mission.rewardText != null)
        {
            mission.rewardText.text = mission.roseReward.ToString();
        }

        if (mission.claimButton != null)
        {
            mission.claimButton.gameObject.SetActive(canClaim);
        }

        if (mission.claimButtonText != null)
        {
            mission.claimButtonText.text = canClaim ? "Claim" : "Claim";
        }
    }

    private int GetMissionTarget(MissionUI mission, int level)
    {
        long target = (long)Mathf.Max(1, mission.initialTarget)
                    + (long)Mathf.Max(1, mission.targetIncrease) * level;

        return (int)Math.Min(target, int.MaxValue);
    }

    #endregion


    #region SAVE LOAD METHODS

    private void LoadMissionData()
    {
        CheckDailyReset();

        viewedProfiles = LoadStringSet(VIEWED_PROFILES_KEY);
        chattedCharacters = LoadStringSet(CHATTED_CHARACTERS_KEY);
    }

    private void CheckDailyReset()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");

        string lastDate = PlayerPrefs.GetString(MISSION_DATE_KEY, "");

        if (lastDate == today)
            return;

        PlayerPrefs.SetString(MISSION_DATE_KEY, today);

        // Only reset daily message progress.
        PlayerPrefs.SetInt(MESSAGE_PROGRESS_KEY, 0);
        PlayerPrefs.Save();
    }

    private void SaveStringSet(string key, HashSet<string> values)
    {
        StringListWrapper wrapper = new StringListWrapper
        {
            values = new List<string>(values)
        };

        PlayerPrefs.SetString(key, JsonUtility.ToJson(wrapper));
        PlayerPrefs.Save();
    }

    private HashSet<string> LoadStringSet(string key)
    {
        string json = PlayerPrefs.GetString(key, "");

        if (string.IsNullOrEmpty(json))
            return new HashSet<string>();

        try
        {
            StringListWrapper wrapper = JsonUtility.FromJson<StringListWrapper>(json);

            return wrapper != null && wrapper.values != null
                ? new HashSet<string>(wrapper.values)
                : new HashSet<string>();
        }
        catch
        {
            return new HashSet<string>();
        }
    }

    [Serializable]
    private class StringListWrapper
    {
        public List<string> values = new List<string>();
    }

    #endregion
}