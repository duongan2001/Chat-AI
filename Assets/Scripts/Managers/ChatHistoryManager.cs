using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class ChatHistoryManager : MonoBehaviour
{
    #region VARIABLES

    public static ChatHistoryManager Instance;

    [SerializeField]
    [Header("--- CHARACTER DATA SCRIPTABLE OBJECTS ---")]
    private CharacterDataSO[] characterDatas;

    [Header("--- CHAT HISTORY DATAS ---")]
    [SerializeField]
    private List<ChatHistory> chatHistories = new List<ChatHistory>();

    public List<int> chatHistoryIds = new List<int>();

    private const string HISTORY_FOLDER_NAME = "ChatHistories";

    private string HistoryDirectory => Path.Combine(Application.persistentDataPath, HISTORY_FOLDER_NAME);

    #endregion


    #region UNITY METHODS

    private void Awake()
    {
        SetupInstance();
    }

    private void Start()
    {
        if (Instance != this)
            return;

        if ((characterDatas == null || characterDatas.Length == 0) && CharacterDataManager.Instance != null)
        {
            characterDatas = CharacterDataManager.Instance.characterDatas;
        }

        LoadCharacterChatHistoryDatas();
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


    #region INITIALIZATION METHODS

    private void LoadCharacterChatHistoryDatas()
    {
        if (characterDatas == null)
        {
            Debug.LogWarning("[ChatHistoryManager] Character data is null.");
            return;
        }

        Directory.CreateDirectory(HistoryDirectory);

        chatHistories = new List<ChatHistory>(new ChatHistory[characterDatas.Length]);
        chatHistoryIds = new List<int>();

        for (int i = 0; i < characterDatas.Length; i++)
        {
            if (characterDatas[i] == null)
                continue;

            if (!IsHistoryFileExists(i))
                continue;

            ChatHistory history = LoadChatHistory(i);

            if (history == null)
                continue;

            chatHistories[i] = history;
            chatHistoryIds.Add(i);
        }

        Debug.Log($"[ChatHistoryManager] Loaded {chatHistoryIds.Count} chat histories.");
    }

    #endregion


    #region FILE PATH METHODS

    private string GetFilePath(int characterDataIndex)
    {
        if (characterDatas == null ||
            characterDataIndex < 0 ||
            characterDataIndex >= characterDatas.Length ||
            characterDatas[characterDataIndex] == null)
        {
            return null;
        }

        int characterId = characterDatas[characterDataIndex].characterId;

        return Path.Combine(HistoryDirectory, $"chat_history_{characterId}.json");
    }

    public bool IsHistoryFileExists(int characterDataIndex)
    {
        string filePath = GetFilePath(characterDataIndex);

        return !string.IsNullOrEmpty(filePath) && File.Exists(filePath);
    }

    #endregion


    #region CHAT HISTORY FILE METHODS

    public ChatHistory NewChatHistoryFile(int characterDataIndex)
    {
        string filePath = GetFilePath(characterDataIndex);

        if (string.IsNullOrEmpty(filePath))
            return null;

        ChatHistory history = new ChatHistory
        {
            messages = new List<Message>(),
            lastMessageTimestamp = 0
        };

        SaveChatHistory(characterDataIndex, history);

        return history;
    }

    public void SaveChatHistory(int characterDataIndex, ChatHistory chatHistory)
    {
        if (chatHistory == null)
            return;

        string filePath = GetFilePath(characterDataIndex);

        if (string.IsNullOrEmpty(filePath))
            return;

        try
        {
            Directory.CreateDirectory(HistoryDirectory);

            if (chatHistory.messages == null)
                chatHistory.messages = new List<Message>();

            string json = JsonUtility.ToJson(chatHistory, true);

            File.WriteAllText(filePath, json);

            if (chatHistories == null)
                chatHistories = new List<ChatHistory>();

            while (chatHistories.Count <= characterDataIndex)
            {
                chatHistories.Add(null);
            }

            chatHistories[characterDataIndex] = chatHistory;

            if (chatHistoryIds == null)
                chatHistoryIds = new List<int>();

            if (!chatHistoryIds.Contains(characterDataIndex))
            {
                chatHistoryIds.Add(characterDataIndex);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ChatHistoryManager] Save failed: {e.Message}");
        }
    }

    public void UpdateChatHistoryMessages(int characterDataIndex, List<Message> messages)
    {
        ChatHistory history = LoadChatHistory(characterDataIndex);

        if (history == null)
        {
            history = new ChatHistory
            {
                messages = new List<Message>(),
                lastMessageTimestamp = 0
            };
        }

        history.messages = messages != null ? new List<Message>(messages) : new List<Message>();

        SaveChatHistory(characterDataIndex, history);
    }

    public ChatHistory LoadChatHistory(int characterDataIndex)
    {
        string filePath = GetFilePath(characterDataIndex);

        if (string.IsNullOrEmpty(filePath))
            return null;

        if (!File.Exists(filePath))
            return null;

        try
        {
            string json = File.ReadAllText(filePath);

            if (string.IsNullOrWhiteSpace(json))
                return null;

            ChatHistory history =
                JsonUtility.FromJson<ChatHistory>(json);

            if (history == null)
                return null;

            if (history.messages == null)
            {
                history.messages = new List<Message>();
            }

            return history;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ChatHistoryManager] Load failed: {e.Message}");

            return null;
        }
    }

    #endregion


    #region MESSAGE CONVERSION METHODS

    public ChatHistory ConvertListMessageToChatHistory(List<Message> messageList)
    {
        ChatHistory history = new ChatHistory
        {
            messages = messageList != null ? new List<Message>(messageList) : new List<Message>(),

            lastMessageTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        return history;
    }

    public List<Message> ConvertChatHistoryToListMessage(ChatHistory chatHistory)
    {
        if (chatHistory == null || chatHistory.messages == null)
        {
            return new List<Message>();
        }

        return new List<Message>(chatHistory.messages);
    }

    #endregion


    #region CHAT HISTORY QUERY METHODS

    public int GetMessageCount(int characterDataIndex)
    {
        ChatHistory history = LoadChatHistory(characterDataIndex);

        if (history == null || history.messages == null)
            return 0;

        return history.messages.Count;
    }

    public string GetLast10Messages(int characterDataIndex)
    {
        ChatHistory history = LoadChatHistory(characterDataIndex);

        if (history == null ||
            history.messages == null ||
            history.messages.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder sb = new StringBuilder();

        int startIndex = Mathf.Max(0, history.messages.Count - 10);

        for (int i = startIndex; i < history.messages.Count; i++)
        {
            Message message = history.messages[i];

            if (message == null)
                continue;

            string role = message.isMessageFromUser ? "User" : "Assistant";

            string content = message.GetMessageTextContent();

            if (string.IsNullOrWhiteSpace(content))
            {
                if (!string.IsNullOrWhiteSpace(message.GetMessagePhotoFilePath()))
                {
                    content = "[Photo]";
                }
                else
                {
                    continue;
                }
            }

            sb.AppendLine($"{role}: {content}");
        }

        return sb.ToString().TrimEnd();
    }

    public string GetLastMessageText(int characterDataIndex)
    {
        ChatHistory history = LoadChatHistory(characterDataIndex);

        if (history == null ||
            history.messages == null ||
            history.messages.Count == 0)
        {
            return string.Empty;
        }

        for (int i = history.messages.Count - 1; i >= 0; i--)
        {
            Message message = history.messages[i];

            if (message == null)
                continue;

            string text = message.GetMessageTextContent();

            if (!string.IsNullOrWhiteSpace(text))
                return text;

            if (!string.IsNullOrWhiteSpace(
                message.GetMessagePhotoFilePath()))
            {
                return "[Photo]";
            }
        }

        return string.Empty;
    }

    public void SetLastMessageTimestamp(int characterDataIndex, long timestamp)
    {
        ChatHistory history = LoadChatHistory(characterDataIndex);

        if (history == null)
        {
            history = NewChatHistoryFile(characterDataIndex);
        }

        if (history == null)
            return;

        history.lastMessageTimestamp = timestamp;

        SaveChatHistory(characterDataIndex, history);
    }

    #endregion


    #region CHAT HISTORY ID METHODS

    public void UpdateChatHistoryIds()
    {
        if (chatHistoryIds == null)
            chatHistoryIds = new List<int>();
        else
            chatHistoryIds.Clear();

        if (characterDatas == null)
            return;

        for (int i = 0; i < characterDatas.Length; i++)
        {
            if (IsHistoryFileExists(i))
            {
                chatHistoryIds.Add(i);
            }
        }
    }

    #endregion


    #region DELETE METHODS

    public void DeleteAllJsonFiles()
    {
        try
        {
            if (Directory.Exists(HistoryDirectory))
            {
                string[] files = Directory.GetFiles(HistoryDirectory, "*.json");

                foreach (string file in files)
                {
                    File.Delete(file);
                }
            }

            if (chatHistories != null)
                chatHistories.Clear();

            if (chatHistoryIds != null)
                chatHistoryIds.Clear();

            Debug.Log("[ChatHistoryManager] All chat histories deleted.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ChatHistoryManager] Delete failed: {e.Message}");
        }
    }

    #endregion
}
