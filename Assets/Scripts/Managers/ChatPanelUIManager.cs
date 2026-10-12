using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatPanelUIManager : PanelBaseUIController
{
    #region VARIABLES

    [SerializeField]
    [Header("--- CHARACTER DATA SCRIPTABLE ---")]
    private CharacterDataSO characterData;

    private int currentCharId = -1;
    private int charWillBeShowId = -1;

    [Header("--- BACKGROUND IMAGE ---")]
    [SerializeField] private Image backgroundImage;

    private Coroutine setBackgroundImageCoroutine;

    [Header("--- CHARACTER AFFECTION LEVEL ---")]
    [SerializeField] private Image characterAffectionProgressBar;

    [Header("--- NUMBER OF FLOWER ---")]
    [SerializeField] private Text numberOfFlowerText;

    [Header("--- DAILY CHAT REMAIN ---")]
    [SerializeField] private Text dailyChatRemainText;

    [Header("--- CHAT MESSAGE PREFAB ---")]
    [SerializeField] private GameObject userBubbleChatTextPrefab;
    [SerializeField] private GameObject aiBubbleChatTextPrefab;
    [SerializeField] private GameObject aiBubbleChatReplyingPrefab;
    [SerializeField] private GameObject currentChatReplyingObject;
    [SerializeField] private GameObject aiBubbleChatPhotoPrefab;

    [Header("--- CHAT INPUT FIELD ---")]
    [SerializeField] private GameObject chatMessageContents;

    [Header("--- USER CHAT INPUT FIELD ---")]
    [SerializeField] private InputField userInputField;

    [Header("--- USER ACTIONS ---")]
    [SerializeField] private PanelBaseUIController playerActionMenu1;
    [SerializeField] private PanelBaseUIController playerActionMenu2;
    [SerializeField] private PanelBaseUIController outfitMenu;

    private string requestPhotoText = "<i>Send me a photo</i>";

    [Header("--- CHAT SETTINGS ---")]
    [SerializeField] private float aiReplyDelay = 0.3f;
    [SerializeField] private float backgroundCheckInterval = 1f;

    [Header("--- AFFECTION SETTINGS ---")]
    [SerializeField] private int affectionBonusPerMessage = 1;

    private FridayAIChat fridayAI;

    private bool isPanelVisible;


    #endregion


    #region UNITY METHODS

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        SubscribeFridayAI();
    }

    private void OnDestroy()
    {
        UnsubscribeFridayAI();
    }

    #endregion


    #region FRIDAY AI METHODS

    private void SubscribeFridayAI()
    {
        FridayAIChat instance = FridayAIChat.Instance;
        if (instance == null || fridayAI == instance) return;
        UnsubscribeFridayAI();
        fridayAI = instance;
        fridayAI.OnCharacterReplyReceived += OnFridayAIReply;
        fridayAI.OnCharacterError += OnFridayAIError;
        fridayAI.OnRequestStateChanged += OnFridayAIRequestStateChanged;
    }

    private void UnsubscribeFridayAI()
    {
        if (fridayAI == null) return;
        fridayAI.OnCharacterReplyReceived -= OnFridayAIReply;
        fridayAI.OnCharacterError -= OnFridayAIError;
        fridayAI.OnRequestStateChanged -= OnFridayAIRequestStateChanged;
        fridayAI = null;
    }

    private void OnFridayAIReply(int characterIndex, string reply)
    {
        if (characterIndex != currentCharId) return;
        // FridayAIChat already saved the reply to ChatHistoryManager.
        // Rebuild from disk to avoid duplicate bubbles after closing/reopening.
        if (isPanelVisible) LoadChatHistoryMessages();
        if (isPanelVisible) ShowAIReplying();
    }

    private void OnFridayAIError(int characterIndex, string error)
    {
        Debug.LogError("[ChatPanel] FridayAI error: " + error);
        if (characterIndex == currentCharId) HideAIReplying();
    }

    private void OnFridayAIRequestStateChanged(int characterIndex, bool isProcessing)
    {
        if (characterIndex != currentCharId) return;
        if (isProcessing && isPanelVisible) ShowAIReplying();
        else if (!isProcessing) HideAIReplying();
    }

    #endregion


    #region CHARACTER METHODS

    public void SetCharWillBeShowId(int id)
    {
        charWillBeShowId = id;
    }

    public void LoadCharacterData(int charID)
    {

        if (CharacterDataManager.Instance == null ||
            CharacterDataManager.Instance.characterDatas == null)
            return;

        CharacterDataSO[] characters = CharacterDataManager.Instance.characterDatas;

        if (charID < 0 || charID >= characters.Length)
            return;

        if (characters[charID] == null)
            return;

        if (fridayAI == null)
            SubscribeFridayAI();

        if (fridayAI == null)
            return;

        if (!fridayAI.SetCharacter(charID))
            return;

        HideAIReplying();
        currentCharId = charID;
        charWillBeShowId = charID;
        characterData = characters[charID];

        LoadBackgroundImage();
        LoadCharacterLevelAffection();
        LoadNumberOfFlower();
        LoadDailyChatRemain();
        LoadChatHistoryMessages();
        CheckOpeningMessage();

        if (userInputField != null)
        {
            userInputField.text = "";
            userInputField.interactable = true;
        }
    }

    #endregion


    #region AFFECTION METHODS

    private void LoadCharacterLevelAffection()
    {
        if (characterData == null ||
            characterAffectionProgressBar == null)
            return;

        // Uses CharacterDataSO's current affection value.
        // Replace with its saved-affection getter if required.
        int affection = characterData.affectionLevel;

        characterAffectionProgressBar.fillAmount = Mathf.Clamp01(affection / 100f);
    }

    private void IncreaseAndUpdateCharacterLevelAffection(int bonusAmount)
    {
        if (characterData == null || bonusAmount <= 0)
            return;

        // Temporary implementation:
        // The original persistence method has not been provided.
        characterData.affectionLevel = Mathf.Clamp(characterData.affectionLevel + bonusAmount, 0, 100);

        LoadCharacterLevelAffection();
    }

    #endregion


    #region FLOWER METHODS

    private void LoadNumberOfFlower()
    {
        if (numberOfFlowerText == null)
            return;

        int amount = PlayerPrefs.GetInt(PlayerPrefKey.NumberOfRose.ToString(), 0);

        numberOfFlowerText.text = amount.ToString();
    }

    #endregion


    #region DAILY CHAT METHODS

    private void LoadDailyChatRemain()
    {
        if (dailyChatRemainText == null)
            return;

        int remaining = PlayerPrefs.GetInt(PlayerPrefKey.DailyChatRemain.ToString(), 0);

        dailyChatRemainText.text = remaining.ToString();
    }

    #endregion


    #region BACKGROUND IMAGE METHODS

    public void LoadBackgroundImage()
    {
        if (setBackgroundImageCoroutine != null)
        {
            StopCoroutine(setBackgroundImageCoroutine);
            setBackgroundImageCoroutine = null;
        }

        if (backgroundImage == null)
            return;

        backgroundImage.sprite = null;
        backgroundImage.enabled = false;

        if (characterData == null)
            return;

        setBackgroundImageCoroutine =
            StartCoroutine(LoadBackgroundImageCrt());
    }

    private IEnumerator LoadBackgroundImageCrt()
    {
        CharacterDataSO target = characterData;

        while (target != null && target == characterData)
        {
            Sprite sprite = null;

            if (target.photoSprites != null &&
                target.photoSprites.Length > 0)
            {
                sprite = target.photoSprites[0];
            }

            if (sprite != null)
            {
                if (backgroundImage != null)
                {
                    backgroundImage.sprite = sprite;
                    backgroundImage.enabled = true;
                }

                setBackgroundImageCoroutine = null;
                yield break;
            }

            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, backgroundCheckInterval));
        }

        setBackgroundImageCoroutine = null;
    }

    #endregion


    #region SEND CHAT METHODS

    public void UserSendChat()
    {
        if (fridayAI == null)
            SubscribeFridayAI();

        if (fridayAI == null ||
            characterData == null ||
            userInputField == null ||
            ChatHistoryManager.Instance == null)
            return;

        string messageText = userInputField.text.Trim();

        if (string.IsNullOrEmpty(messageText))
            return;

        Message message = new Message
        {
            messageType = MessageType.text,
            isMessageFromUser = true,
            textContent = messageText,
            photoFilePath = string.Empty
        };

        SaveMessageToHistory(message);

        CreateChatBubble(message);

        userInputField.text = "";
        ShowAIReplying();

        // The message is already in ChatHistoryManager before requesting.
        fridayAI.UserSendMessage(messageText);
    }

    #endregion


    #region PHOTO METHODS

    public void UserRequestPhoto()
    {
        // Photo generation/request behavior requires its
        // original API and unlock rules.
        Debug.Log("[ChatPanel] Photo request is not configured.");
    }

    #endregion


    #region USER ACTION METHODS

    public void UserDoAction(string action)
    {
        if (string.IsNullOrWhiteSpace(action))
            return;

        Debug.Log("[ChatPanel] User action: " + action);

        // Action-specific behavior can be connected here.
    }

    #endregion


    #region AI REPLY METHODS

    // Compatibility method for other UnityEvents. FridayAIChat owns API reply saving.
    public void AIReplyChat(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply)) return;
        Message message = new Message
        {
            messageType = MessageType.text,
            isMessageFromUser = false,
            textContent = FormatAIMessage(reply),
            photoFilePath = string.Empty
        };
        SaveMessageToHistory(message);
        if (isPanelVisible) CreateChatBubble(message);
    }

    public string FormatAIMessage(string text)
    {
        return string.IsNullOrEmpty(text) ? string.Empty : text.Trim();
    }

    // Kept for compatibility with the original method signature.
    private IEnumerator ShowAIReplyingChatDelay()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, aiReplyDelay));
    }

    #endregion


    #region CHAT HISTORY METHODS

    private void SaveMessageToHistory(Message message)
    {
        if (message == null ||
            currentCharId < 0 ||
            ChatHistoryManager.Instance == null)
            return;

        ChatHistoryManager.Instance.AddChatMessage(currentCharId, message);
    }

    private void LoadChatHistoryMessages()
    {
        ClearChatBubbles();

        if (ChatHistoryManager.Instance == null ||
            currentCharId < 0)
            return;

        ChatHistory history = ChatHistoryManager.Instance.LoadChatHistory(currentCharId);

        if (history == null || history.messages == null)
            return;

        foreach (Message message in history.messages)
        {
            if (message == null)
                continue;

            CreateChatBubble(message);
        }

        if (fridayAI != null && fridayAI.IsRequesting &&
            fridayAI.CurrentCharacterDataIndex == currentCharId && isPanelVisible)
            ShowAIReplying();
    }

    private void CheckOpeningMessage()
    {
        if (fridayAI == null || ChatHistoryManager.Instance == null) return;
        ChatHistory history = ChatHistoryManager.Instance.LoadChatHistory(currentCharId);
        if (history == null || history.messages == null || history.messages.Count == 0)
            fridayAI.GenerateOpeningMessage();
    }

    #endregion


    #region CHAT BUBBLE METHODS

    private void CreateChatBubble(Message message)
    {
        if (message == null || chatMessageContents == null)
            return;

        GameObject prefab;

        if (message.messageType == MessageType.image)
        {
            if (message.isMessageFromUser)
            {
                // No user photo prefab in the current skeleton.
                return;
            }

            prefab = aiBubbleChatPhotoPrefab;
        }
        else
        {
            prefab = message.isMessageFromUser ? userBubbleChatTextPrefab : aiBubbleChatTextPrefab;
        }

        if (prefab == null)
            return;

        GameObject bubble = Instantiate(prefab, chatMessageContents.transform);

        if (message.messageType == MessageType.text)
        {
            SetBubbleText(bubble, message.GetMessageTextContent());
        }
        MoveAIReplyingToBottom();
    }

    private void SetBubbleText(GameObject bubble, string content)
    {
        if (bubble == null)
            return;

        // Supports legacy Unity UI Text.
        Text legacyText = bubble.GetComponentInChildren<Text>(true);

        if (legacyText != null)
        {
            legacyText.text = content;
            return;
        }

        // Supports TextMeshPro.
        TMPro.TMP_Text tmpText = bubble.GetComponentInChildren<TMPro.TMP_Text>(true);

        if (tmpText != null)
        {
            tmpText.text = content;
            return;
        }

        Debug.LogWarning("[ChatPanel] Bubble prefab has no Text component.");
    }

    private void ClearChatBubbles()
    {
        if (chatMessageContents == null)
            return;

        Transform container = chatMessageContents.transform;

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Transform child = container.GetChild(i);

            if (currentChatReplyingObject != null &&
                child.gameObject == currentChatReplyingObject)
                continue;

            Destroy(child.gameObject);
        }
    }

    #endregion


    #region AI REPLYING METHODS

    private void ShowAIReplying()
    {
        if (currentChatReplyingObject != null)
        {
            MoveAIReplyingToBottom();
            return;
        }

        if (aiBubbleChatReplyingPrefab == null ||
            chatMessageContents == null)
            return;

        currentChatReplyingObject = Instantiate(aiBubbleChatReplyingPrefab, chatMessageContents.transform);
        MoveAIReplyingToBottom();
    }

    private void MoveAIReplyingToBottom()
    {
        if (currentChatReplyingObject != null)
            currentChatReplyingObject.transform.SetAsLastSibling();
    }

    private void HideAIReplying()
    {
        if (currentChatReplyingObject == null)
            return;

        Destroy(currentChatReplyingObject);
        currentChatReplyingObject = null;
    }

    #endregion


    #region PANEL METHODS

    public override void Show()
    {
        base.Show();

        isPanelVisible = true;

        if (fridayAI == null)
            SubscribeFridayAI();

        if (charWillBeShowId >= 0 &&
            charWillBeShowId != currentCharId)
        {
            LoadCharacterData(charWillBeShowId);
        }
        else if (currentCharId >= 0)
        {
            LoadChatHistoryMessages();
            CheckOpeningMessage();
        }
    }

    public override void ShowDelay()
    {
        base.ShowDelay();

        isPanelVisible = true;

        if (fridayAI == null)
            SubscribeFridayAI();

        if (charWillBeShowId >= 0 &&
            charWillBeShowId != currentCharId)
        {
            LoadCharacterData(charWillBeShowId);
        }
        else if (currentCharId >= 0)
        {
            LoadChatHistoryMessages();
            CheckOpeningMessage();
        }
    }

    public override void Hide()
    {
        isPanelVisible = false;

        if (setBackgroundImageCoroutine != null)
        {
            StopCoroutine(setBackgroundImageCoroutine);
            setBackgroundImageCoroutine = null;
        }

        base.Hide();
    }

    #endregion


    #region POPUP METHODS

    public void ShowPopup(string popupName)
    {
        if (string.IsNullOrEmpty(popupName))
            return;

        switch (popupName.ToLowerInvariant())
        {
            case "action1":
                if (playerActionMenu1 != null)
                    playerActionMenu1.Show();
                break;

            case "action2":
                if (playerActionMenu2 != null)
                    playerActionMenu2.Show();
                break;

            case "outfit":
                if (outfitMenu != null)
                    outfitMenu.Show();
                break;

            default:
                Debug.LogWarning("[ChatPanel] Unknown popup: " + popupName);
                break;
        }
    }

    #endregion
}
