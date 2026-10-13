using System.Collections.Generic;
using UnityEngine;

public class ChatboxPanelUIManager : PanelBaseUIController
{
    #region VARIABLES

    [Header("--- CHATBOX ITEM LIST ---")]
    [SerializeField] private GameObject chatboxItemPrefab;
    [SerializeField] private GameObject emptyChatboxItem;
    [SerializeField] private RectTransform chatboxItemContainer;

    private readonly List<GameObject> spawnedItems = new List<GameObject>();

    #endregion


    #region CHATBOX ITEM METHODS

    private void LoadChatBoxItemList()
    {
        ClearChatBoxItemList();

        if (emptyChatboxItem != null)
            emptyChatboxItem.SetActive(false);

        if (chatboxItemContainer == null ||
            chatboxItemPrefab == null ||
            CharacterDataManager.Instance == null ||
            ChatHistoryManager.Instance == null)
        {
            return;
        }

        CharacterDataSO[] characters = CharacterDataManager.Instance.characterDatas;

        if (characters == null)
            return;

        List<ChatboxEntry> entries = new List<ChatboxEntry>();

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == null)
                continue;

            ChatHistory history = ChatHistoryManager.Instance.LoadChatHistory(i);

            if (history == null ||
                history.messages == null ||
                history.messages.Count == 0)
                continue;

            entries.Add(new ChatboxEntry
            {
                characterIndex = i,
                characterData = characters[i],
                lastMessageTimestamp = history.lastMessageTimestamp
            });
        }

        // Sort newest conversations first.
        entries.Sort((a, b) => b.lastMessageTimestamp.CompareTo(a.lastMessageTimestamp));

        if (entries.Count == 0)
        {
            if (emptyChatboxItem != null)
                emptyChatboxItem.SetActive(true);

            return;
        }

        foreach (ChatboxEntry entry in entries)
        {
            GameObject item = Instantiate(chatboxItemPrefab, chatboxItemContainer);

            item.SetActive(true);
            spawnedItems.Add(item);

            ChatboxItem chatboxItem = item.GetComponent<ChatboxItem>();

            if (chatboxItem != null)
            {
                chatboxItem.SetChatItemCharacterData(entry.characterData);
            }
        }
    }

    private void ClearChatBoxItemList()
    {
        foreach (GameObject item in spawnedItems)
        {
            if (item != null)
                Destroy(item);
        }

        spawnedItems.Clear();
    }

    #endregion


    #region PANEL METHODS

    public override void Show()
    {
        base.Show();
        LoadChatBoxItemList();
    }

    public override void Hide()
    {
        base.Hide();
    }

    public override void ShowDelay()
    {
        base.ShowDelay();
        LoadChatBoxItemList();
    }

    #endregion


    #region DATA CLASSES

    private class ChatboxEntry
    {
        public int characterIndex;
        public CharacterDataSO characterData;
        public long lastMessageTimestamp;
    }

    #endregion
}
