using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ChatboxItem : MonoBehaviour
{
    #region VARIABLES

    [Header("--- CHARACTER DATA SCRIPTABLE ---")]
    [SerializeField] private CharacterDataSO characterData;

    private int characterDataIndex = -1;

    [Header("--- CHATBOX ITEM DEFAULT PHOTO ---")]
    [SerializeField] private Image defaultPhoto;

    private Coroutine setDefaultImageCoroutine;

    [Header("--- CHATBOX ITEM CHARACTER INFO ---")]
    [SerializeField] private Text characterInfoText;

    [Header("--- CHATBOX ITEM LATEST MESSAGE ---")]
    [SerializeField] private Text latestMessageText;

    private const float IMAGE_CHECK_INTERVAL = 1f;

    #endregion


    #region UNITY METHODS

    private void OnDisable()
    {
        StopDefaultImageCoroutine();
    }

    private void OnDestroy()
    {
        StopDefaultImageCoroutine();
    }

    #endregion


    #region CHARACTER DATA METHODS

    public void SetChatItemCharacterData(CharacterDataSO data)
    {
        StopDefaultImageCoroutine();

        characterData = data;
        characterDataIndex = GetCharacterDataIndex(data);

        SetChatboxItemDefaultImage();
        SetChatboxItemCharacterInfo();
        LoadChatboxLatestMessage();
    }

    private int GetCharacterDataIndex(CharacterDataSO data)
    {
        if (data == null ||
            CharacterDataManager.Instance == null ||
            CharacterDataManager.Instance.characterDatas == null)
            return -1;

        CharacterDataSO[] characters = CharacterDataManager.Instance.characterDatas;

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == data)
                return i;
        }

        return -1;
    }

    #endregion


    #region IMAGE METHODS

    public void SetChatboxItemDefaultImage()
    {
        StopDefaultImageCoroutine();

        if (defaultPhoto == null)
            return;

        defaultPhoto.sprite = null;
        defaultPhoto.enabled = false;

        if (characterData == null)
            return;

        setDefaultImageCoroutine = StartCoroutine(SetChatboxItemDefaultImageCrt());
    }

    private IEnumerator SetChatboxItemDefaultImageCrt()
    {
        CharacterDataSO target = characterData;

        while (target != null && target == characterData)
        {
            if (target.photoSprites != null &&
                target.photoSprites.Length > 0 &&
                target.photoSprites[0] != null)
            {
                if (defaultPhoto != null)
                {
                    defaultPhoto.sprite = target.photoSprites[0];
                    defaultPhoto.enabled = true;
                }

                setDefaultImageCoroutine = null;
                yield break;
            }

            yield return new WaitForSecondsRealtime(IMAGE_CHECK_INTERVAL);
        }

        setDefaultImageCoroutine = null;
    }

    private void StopDefaultImageCoroutine()
    {
        if (setDefaultImageCoroutine == null)
            return;

        StopCoroutine(setDefaultImageCoroutine);
        setDefaultImageCoroutine = null;
    }

    #endregion


    #region CHARACTER INFO METHODS

    public void SetChatboxItemCharacterInfo()
    {
        if (characterInfoText == null)
            return;

        if (characterData == null)
        {
            characterInfoText.text = string.Empty;
            return;
        }

        characterInfoText.text = $"{characterData.characterName}";
    }

    #endregion


    #region LATEST MESSAGE METHODS

    public void LoadChatboxLatestMessage()
    {
        if (latestMessageText == null)
            return;

        latestMessageText.text = string.Empty;

        if (characterDataIndex < 0 ||
            ChatHistoryManager.Instance == null)
            return;

        ChatHistory history =
            ChatHistoryManager.Instance.LoadChatHistory(characterDataIndex);

        if (history == null ||
            history.messages == null ||
            history.messages.Count == 0)
            return;

        for (int i = history.messages.Count - 1; i >= 0; i--)
        {
            Message message = history.messages[i];

            if (message == null)
                continue;

            if (message.messageType == MessageType.image)
            {
                latestMessageText.text = "Photo";
                return;
            }

            string content = message.GetMessageTextContent();

            if (!string.IsNullOrWhiteSpace(content))
            {
                latestMessageText.text = content;
                return;
            }
        }
    }

    #endregion


    #region BUTTON METHODS

    public void OnChatboxButtonClick()
    {
        if (characterDataIndex < 0)
        {
            Debug.LogWarning("[ChatboxItem] Invalid character index.");
            return;
        }

        if (UIManager.Instance == null ||
            UIManager.Instance.chatPanel == null)
        {
            Debug.LogWarning("[ChatboxItem] ChatPanel not found.");
            return;
        }

        ChatPanelUIManager chatPanel = UIManager.Instance.chatPanel;

        chatPanel.SetCharWillBeShowId(characterDataIndex);
        chatPanel.Show();
    }

    #endregion
}
