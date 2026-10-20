using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterProfilePanelUIManager : PanelBaseUIController
{
    #region VARIABLES

    [Header("--- CHARACTER DATA SCRIPTABLE ---")]
    [SerializeField] private CharacterDataSO characterData;
    private int currentCharId = -1;
    private int charWillBeShowId = -1;

    [Header("--- CHAT ITEM STATE ---")]
    [SerializeField] private Color stateNewColor = Color.white;
    [SerializeField] private Color stateHotColor = Color.white;
    [SerializeField] private Image stateBackgroundImage;
    [SerializeField] private Text stateText;

    [Header("--- CHARACTER AVATAR ---")]
    [SerializeField] private Image[] characterAvatarImages;

    [Header("--- BACKGROUND IMAGE ---")]
    [SerializeField] private Image backgroundImage;
    private Coroutine setImagesCoroutine;

    [Header("--- CHARACTER MESSAGE COUNT ---")]
    [SerializeField] private Text messageCountText;

    [Header("--- CHARACTER INFO ---")]
    [SerializeField] private Text characterInfoText;

    [Header("--- CHARACTER SHORT DESCRIPTION ---")]
    [SerializeField] private TextMeshProUGUI shortDescriptionText;

    [Header("--- CHARACTER TAGS ---")]
    [SerializeField] private Image[] characterTags;
    [SerializeField] private Text[] characterTagTexts;

    [Header("--- CHARACTER PHOTOS ---")]
    [SerializeField] private RectTransform characterPhotoContainer;
    [SerializeField] private GameObject characterPhotoPrefab;

    private readonly List<CharaterPhoto> spawnedPhotoItems = new List<CharaterPhoto>();

    private const int NEW_CHARACTER_COUNT = 6;
    private const float PHOTO_CHECK_INTERVAL = 1f;

    #endregion


    #region UNITY METHODS

    private void OnDisable()
    {
        StopImageCoroutine();
    }

    #endregion


    #region CHARACTER DATA METHODS

    public void SetCharacterData(CharacterDataSO data)
    {
        StopImageCoroutine();

        characterData = data;
        currentCharId = GetCharacterDataIndex(data);
        charWillBeShowId = currentCharId;

        ClearCharacterImages();
        ClearCharacterPhotoList();

        SetCharacterState();
        SetCharacterMessageCount();
        SetCharacterInfo();
        SetCharacterDescription();
        SetCharacterTags();
        SetCharacterImages();
        LoadCharacterPhotoList();
    }

    private int GetCharacterDataIndex(CharacterDataSO data)
    {
        if (data == null ||
            CharacterDataManager.Instance == null ||
            CharacterDataManager.Instance.characterDatas == null)
        {
            return -1;
        }

        CharacterDataSO[] characters = CharacterDataManager.Instance.characterDatas;

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == data)
                return i;
        }

        return -1;
    }

    #endregion


    #region CHARACTER STATE METHODS

    public void SetCharacterState()
    {
        if (stateText == null && stateBackgroundImage == null)
            return;

        bool isNew = currentCharId >= 0 && currentCharId < NEW_CHARACTER_COUNT;

        if (stateText != null)
            stateText.text = isNew ? "New" : "Hot";

        if (stateBackgroundImage != null)
        {
            stateBackgroundImage.color = isNew ? stateNewColor : stateHotColor;
        }
    }

    #endregion


    #region CHARACTER MESSAGE COUNT METHODS

    public void SetCharacterMessageCount()
    {
        if (messageCountText == null)
            return;

        messageCountText.text = characterData != null ? characterData.messageCount ?? string.Empty : "0K";
    }

    #endregion


    #region CHARACTER IMAGE METHODS

    public void SetCharacterImages()
    {
        StopImageCoroutine();

        if (characterData == null)
        {
            ClearCharacterImages();
            return;
        }

        setImagesCoroutine = StartCoroutine(SetCharacterDefaultImagesCrt());
    }

    private IEnumerator SetCharacterDefaultImagesCrt()
    {
        while (characterData != null)
        {
            Sprite avatar = GetPhotoSprite(0);

            if (avatar != null)
            {
                if (characterAvatarImages != null)
                {
                    foreach (Image image in characterAvatarImages)
                    {
                        if (image == null)
                            continue;

                        image.sprite = avatar;
                        image.enabled = true;
                    }
                }

                if (backgroundImage != null)
                {
                    backgroundImage.sprite = avatar;
                    backgroundImage.enabled = true;
                }

                setImagesCoroutine = null;
                yield break;
            }

            yield return new WaitForSecondsRealtime(PHOTO_CHECK_INTERVAL);
        }

        setImagesCoroutine = null;
    }

    private Sprite GetPhotoSprite(int photoIndex)
    {
        if (characterData == null ||
            characterData.photoSprites == null ||
            photoIndex < 0 ||
            photoIndex >= characterData.photoSprites.Length)
        {
            return null;
        }

        return characterData.photoSprites[photoIndex];
    }

    private void ClearCharacterImages()
    {
        if (characterAvatarImages != null)
        {
            foreach (Image image in characterAvatarImages)
            {
                if (image == null)
                    continue;

                image.sprite = null;
                image.enabled = false;
            }
        }

        if (backgroundImage != null)
        {
            backgroundImage.sprite = null;
            backgroundImage.enabled = false;
        }
    }

    private void StopImageCoroutine()
    {
        if (setImagesCoroutine == null)
            return;

        StopCoroutine(setImagesCoroutine);
        setImagesCoroutine = null;
    }

    #endregion


    #region CHARACTER INFO METHODS

    public void SetCharacterInfo()
    {
        if (characterInfoText == null)
            return;

        if (characterData == null)
        {
            characterInfoText.text = string.Empty;
            return;
        }

        characterInfoText.text = $"{characterData.characterName}, {characterData.age}";
    }

    public void SetCharacterDescription()
    {
        if (shortDescriptionText == null)
            return;

        shortDescriptionText.text = characterData != null ? characterData.GetRelationshipGoalString() : string.Empty;
    }

    #endregion


    #region CHARACTER TAG METHODS

    public void SetCharacterTags()
    {
        if (characterTagTexts == null)
            return;

        int tagCount = characterData != null && characterData.personalities != null ? characterData.personalities.Count : 0;

        for (int i = 0; i < characterTagTexts.Length; i++)
        {
            bool shouldShow = i < tagCount;

            if (characterTags != null && i < characterTags.Length && characterTags[i] != null)
            {
                characterTags[i].gameObject.SetActive(shouldShow);
            }

            if (characterTagTexts[i] == null)
                continue;

            characterTagTexts[i].text = shouldShow ? characterData.GetPersonalityString(i) : string.Empty;
        }
    }

    #endregion


    #region CHARACTER PHOTO LIST METHODS

    public void LoadCharacterPhotoList()
    {
        ClearCharacterPhotoList();

        if (characterData == null ||
            characterPhotoContainer == null ||
            characterPhotoPrefab == null)
        {
            return;
        }

        characterData.SetListPhotoNumberOfElement();

        int photoCount = Mathf.Max(0, characterData.numberOfPhoto);

        for (int i = 0; i < photoCount; i++)
        {
            GameObject photoObject = Instantiate(characterPhotoPrefab, characterPhotoContainer);

            CharaterPhoto photoItem = photoObject.GetComponent<CharaterPhoto>();

            if (photoItem == null)
            {
                Debug.LogWarning("[CharacterProfile] Character photo prefab missing CharaterPhoto component.");

                Destroy(photoObject);
                continue;
            }

            photoItem.SetCharacterData(characterData);
            photoItem.SetPhotoId(i);

            spawnedPhotoItems.Add(photoItem);

            photoObject.SetActive(true);
            photoItem.SetPhoto();
        }
    }

    public void RefreshCharacterPhotoList()
    {
        if (characterData == null)
            return;

        if (spawnedPhotoItems.Count != characterData.numberOfPhoto)
        {
            LoadCharacterPhotoList();
            return;
        }

        foreach (CharaterPhoto photoItem in spawnedPhotoItems)
        {
            if (photoItem == null)
                continue;

            photoItem.SetPhoto();
        }
    }

    private void ClearCharacterPhotoList()
    {
        foreach (CharaterPhoto photoItem in spawnedPhotoItems)
        {
            if (photoItem != null)
            {
                Destroy(photoItem.gameObject);
            }
        }

        spawnedPhotoItems.Clear();
    }

    #endregion


    #region PANEL METHODS

    public override void Show()
    {
        base.Show();

        SetCharacterState();
        SetCharacterMessageCount();
        SetCharacterInfo();
        SetCharacterDescription();
        SetCharacterTags();
        SetCharacterImages();

        RefreshCharacterPhotoList();
    }

    public override void Hide()
    {
        StopImageCoroutine();
        base.Hide();
    }

    public override void ShowDelay()
    {
        base.ShowDelay();

        RefreshCharacterPhotoList();
    }

    #endregion


    #region BUTTON METHODS

    public void OnChatButtonClick()
    {
        if (characterData == null)
            return;

        if (currentCharId < 0)
        {
            currentCharId = GetCharacterDataIndex(characterData);
        }

        if (currentCharId < 0)
        {
            Debug.LogWarning("[CharacterProfile] Invalid character index.");
            return;
        }

        if (UIManager.Instance == null ||
            UIManager.Instance.chatPanel == null)
        {
            Debug.LogWarning("[CharacterProfile] ChatPanel not found.");
            return;
        }

        ChatPanelUIManager chatPanel = UIManager.Instance.chatPanel;

        chatPanel.SetCharWillBeShowId(currentCharId);
        chatPanel.Show();

        Hide();
    }


    #endregion
}
