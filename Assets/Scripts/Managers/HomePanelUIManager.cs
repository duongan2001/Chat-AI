
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HomePanelUIManager : PanelBaseUIController
{
    #region VARIABLES

    [Header("--- CHAT ITEMS ---")]
    public List<ChatItemHome> chatItems = new List<ChatItemHome>();

    [Header("--- NUMBER OF CHARACTER ---")]
    [SerializeField] int numberOfCharacter;

    [Header("--- RECOMMEND CHARACTERS ---")]
    [SerializeField] int[] recommendCharacterIds;
    [SerializeField] ChatItemHome[] recommendCharacterChatItems;
    
    [Header("--- DISCOVERY CHARACTERS ---")]
    [SerializeField] Image[] tagImages;
    [SerializeField] Color showingTagColor = Color.white;
    [SerializeField] Color notShowingTagColor = Color.gray;

    [Header("--- DISCOVERY CHARACTERS ---")]
    [SerializeField] GameObject[] allCharacterLines;
    [SerializeField] ChatItemHome[] allCharacterChatItems;
    
    private int canShowCharacterItemNumber;
    private string currentFilter = "All";

    [SerializeField] List<int> hotCharacterIds = new List<int>();
    [SerializeField] List<int> newCharacterIds = new List<int>();
    [SerializeField] List<int> animeCharacterIds = new List<int>();
    [SerializeField] List<int> realisticCharacterIds = new List<int>();
    [SerializeField] List<int> maleCharacterIds = new List<int>();
    [SerializeField] List<int> femaleCharacterIds = new List<int>();
    [SerializeField] List<int> transCharacterIds = new List<int>();

    private CharacterDataSO[] characterDatas;
    private const int NEW_CHARACTER_COUNT = 6;

    #endregion


    #region UNITY METHODS

    private void Awake()
    {
        // PanelBaseUIController initializes its CanvasGroup
        // in its own Awake method.
        base.Awake();

        if (chatItems == null)
            chatItems = new List<ChatItemHome>();
    }

    private void Start()
    {
        InitializeCharacterData();
        RandomRecommendCharacterChatItem();
        SetupCharacterFilterList();
        ShowItemsInHome("All");

        //Test
        Show();
    }

    #endregion


    #region CHARACTER DATA METHODS

    private void InitializeCharacterData()
    {
        if (CharacterDataManager.Instance == null)
        {
            Debug.LogWarning("[HomePanelUIManager] CharacterDataManager is missing.");
            return;
        }

        characterDatas = CharacterDataManager.Instance.characterDatas;

        if (characterDatas == null)
            return;

        numberOfCharacter = characterDatas.Length;
    }

    private bool IsValidCharacterIndex(int index)
    {
        return characterDatas != null &&
               index >= 0 &&
               index < characterDatas.Length &&
               characterDatas[index] != null;
    }

    #endregion


    #region RECOMMEND CHARACTER METHODS

    private void RandomRecommendCharacterChatItem()
    {
        if (characterDatas == null ||
            recommendCharacterChatItems == null)
            return;

        int slotCount = recommendCharacterChatItems.Length;

        if (slotCount == 0)
            return;

        List<int> availableIds = new List<int>();

        for (int i = 0; i < characterDatas.Length; i++)
        {
            if (IsValidCharacterIndex(i))
                availableIds.Add(i);
        }

        // Fisher-Yates shuffle.
        for (int i = availableIds.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);

            int temp = availableIds[i];
            availableIds[i] = availableIds[randomIndex];
            availableIds[randomIndex] = temp;
        }

        recommendCharacterIds = new int[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            ChatItemHome item = recommendCharacterChatItems[i];

            if (i >= availableIds.Count)
            {
                recommendCharacterIds[i] = -1;

                if (item != null)
                    item.gameObject.SetActive(false);

                continue;
            }

            int characterIndex = availableIds[i];

            recommendCharacterIds[i] = characterIndex;

            if (item == null)
                continue;

            item.gameObject.SetActive(true);

            item.SetChatItemCharacterData(characterDatas[characterIndex]);

            if (!chatItems.Contains(item))
                chatItems.Add(item);
        }
    }

    #endregion


    #region CHARACTER FILTER METHODS

    private void SetupCharacterFilterList()
    {
        hotCharacterIds.Clear();
        newCharacterIds.Clear();
        animeCharacterIds.Clear();
        realisticCharacterIds.Clear();
        maleCharacterIds.Clear();
        femaleCharacterIds.Clear();
        transCharacterIds.Clear();

        if (characterDatas == null)
            return;

        for (int i = 0; i < characterDatas.Length; i++)
        {
            if (!IsValidCharacterIndex(i))
                continue;

            CharacterDataSO character = characterDatas[i];

            // First 6 characters are NEW.
            if (i < NEW_CHARACTER_COUNT)
                newCharacterIds.Add(i);
            else
                hotCharacterIds.Add(i);

            // CharacterType enum classification.
            string characterType = character.characterType.ToString().ToLowerInvariant();

            if (characterType.Contains("anime"))
                animeCharacterIds.Add(i);

            if (characterType.Contains("real") || characterType.Contains("realistic"))
            {
                realisticCharacterIds.Add(i);
            }

            // GenderType enum classification.
            string gender = character.gender.ToString().ToLowerInvariant();

            if (gender.Contains("female"))
            {
                femaleCharacterIds.Add(i);
            }
            else if (gender.Contains("other"))
            {
                transCharacterIds.Add(i);
            }
            else if (gender.Contains("male"))
            {
                maleCharacterIds.Add(i);
            }
        }
    }

    public void ShowItemsInHome(string filterBy)
    {
        currentFilter = NormalizeFilter(filterBy);

        ShowCharacterChatItemWithFilter(currentFilter);
        ChangeShowingTagColor(GetFilterTagIndex(currentFilter));
    }

    private string NormalizeFilter(string filterBy)
    {
        if (string.IsNullOrWhiteSpace(filterBy))
            return "All";

        switch (filterBy.Trim().ToLowerInvariant())
        {
            case "hot":
                return "Hot";

            case "new":
                return "New";

            case "anime":
                return "Anime";

            case "real":
            case "realistic":
                return "Realistic";

            case "male":
                return "Male";

            case "female":
                return "Female";

            case "trans":
            case "transgender":
                return "Trans";

            default:
                return "All";
        }
    }

    private List<int> GetFilteredCharacterIds(string filterBy)
    {
        switch (NormalizeFilter(filterBy))
        {
            case "Hot":
                return hotCharacterIds;

            case "New":
                return newCharacterIds;

            case "Anime":
                return animeCharacterIds;

            case "Realistic":
                return realisticCharacterIds;

            case "Male":
                return maleCharacterIds;

            case "Female":
                return femaleCharacterIds;

            case "Trans":
                return transCharacterIds;

            default:
                List<int> allIds = new List<int>();

                if (characterDatas != null)
                {
                    for (int i = 0; i < characterDatas.Length; i++)
                    {
                        if (IsValidCharacterIndex(i))
                            allIds.Add(i);
                    }
                }

                return allIds;
        }
    }

    private void ShowCharacterChatItemWithFilter(string filterBy)
    {
        if (allCharacterChatItems == null)
            return;

        List<int> filteredIds = GetFilteredCharacterIds(filterBy);

        canShowCharacterItemNumber = GetCanShowCharacterItemNumber(filterBy);

        for (int i = 0; i < allCharacterChatItems.Length; i++)
        {
            ChatItemHome item = allCharacterChatItems[i];

            if (item == null)
                continue;

            bool shouldShow = i < canShowCharacterItemNumber && i < filteredIds.Count;

            item.gameObject.SetActive(shouldShow);

            if (!shouldShow)
                continue;

            int characterIndex = filteredIds[i];

            if (!IsValidCharacterIndex(characterIndex))
                continue;

            item.SetChatItemCharacterData(characterDatas[characterIndex]);

            if (!chatItems.Contains(item))
                chatItems.Add(item);
        }

        TurnOnCharacterLines(filterBy);
    }

    private int GetCanShowCharacterItemNumber(string filterBy)
    {
        List<int> filteredIds = GetFilteredCharacterIds(filterBy);

        int slotCount = allCharacterChatItems != null ? allCharacterChatItems.Length : 0;

        return Mathf.Min(filteredIds.Count, slotCount);
    }

    private int GetNumberOfCharacterLine(string filterBy)
    {
        if (allCharacterLines == null || allCharacterLines.Length == 0)
            return 0;

        int itemCount = GetCanShowCharacterItemNumber(filterBy);

        // Assume each line contains 2 character items.
        const int ITEMS_PER_LINE = 2;

        int lineCount = Mathf.CeilToInt((float)itemCount / ITEMS_PER_LINE);

        return Mathf.Min(lineCount, allCharacterLines.Length);
    }

    private void TurnOnCharacterLines(string filterBy)
    {
        if (allCharacterLines == null)
            return;

        int lineCount = GetNumberOfCharacterLine(filterBy);

        for (int i = 0; i < allCharacterLines.Length; i++)
        {
            if (allCharacterLines[i] == null)
                continue;

            allCharacterLines[i].SetActive(i < lineCount);
        }
    }

    #endregion


    #region FILTER TAG METHODS

    private int GetFilterTagIndex(string filterBy)
    {
        switch (NormalizeFilter(filterBy))
        {
            case "Hot": return 1;
            case "New": return 2;
            case "Anime": return 3;
            case "Realistic": return 4;
            case "Male": return 5;
            case "Female": return 6;
            case "Trans": return 7;
            default: return 0;
        }
    }

    private void ChangeShowingTagColor(int tagId)
    {
        if (tagImages == null)
            return;

        for (int i = 0; i < tagImages.Length; i++)
        {
            if (tagImages[i] == null)
                continue;

            tagImages[i].color = i == tagId ? showingTagColor : notShowingTagColor;
        }
    }

    #endregion


    #region REFRESH METHODS

    public void RefreshAllChatItems()
    {
        if (chatItems == null)
            return;

        for (int i = 0; i < chatItems.Count; i++)
        {
            ChatItemHome item = chatItems[i];

            if (item == null || !item.gameObject.activeInHierarchy)
                continue;

            item.SetChatItemState();
            item.SetChatItemMessageCount();
            item.SetChatItemCharacterInfo();
            item.SetChatItemCharacterRelationship();
            item.SetupChatItemLockState();
            item.SetChatItemDefaultImage();
        }
    }

    public void UpdateHomeChatItemsRelationshipLanguage()
    {
        if (chatItems == null)
            return;

        for (int i = 0; i < chatItems.Count; i++)
        {
            ChatItemHome item = chatItems[i];

            if (item == null)
                continue;

            item.SetChatItemCharacterRelationship();
        }
    }

    #endregion


    #region PANEL METHODS

    public override void Show()
    {
        base.Show();

        RefreshAllChatItems();
    }

    public override void ShowDelay()
    {
        base.ShowDelay();

        RefreshAllChatItems();
    }

    public override void Hide()
    {
        base.Hide();
    }

    #endregion
}
