
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatItemHome : MonoBehaviour
{
    #region VARIABLES

    [Header("--- CHARACTER DATA SCRIPTABLE ---")]
    [SerializeField] CharacterDataSO characterData;

    [Header("--- CHAT ITEM STATE ---")]
    [SerializeField] Color stateNewColor = Color.green;
    [SerializeField] Color stateHotColor = Color.red;
    [SerializeField] Image stateBackgroundImage;
    [SerializeField] Text stateText;

    [Header("--- CHAT ITEM MESSAGE COUNT ---")]
    [SerializeField] Text messageCountText;

    [Header("--- CHAT ITEM DEFAULT PHOTO ---")]
    [SerializeField] Image defaultPhoto;
    [SerializeField] Image loadingPhotoEffect;
    private Coroutine setDefaultImageCoroutine;

    [Header("--- CHAT ITEM CHARACTER INFO ---")]
    [SerializeField] Text characterInfoText;

    [Header("--- CHAT ITEM CHARACTER RELATIONSHIP ---")]
    [SerializeField] TextMeshProUGUI characterRelationshipGoalText;

    [Header("--- CHAT ITEM LOCK STATE ---")]
    [SerializeField] Image lockState;

    private int characterDataIndex = -1;
    private const int NEW_CHARACTER_COUNT = 6;
    private const float PHOTO_CHECK_INTERVAL = 1f;

    #endregion


    #region UNITY METHODS

    private void Awake()
    {
        if (loadingPhotoEffect != null)
            loadingPhotoEffect.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (characterData != null)
            SetChatItemCharacterData(characterData);
    }

    private void OnDisable()
    {
        if (setDefaultImageCoroutine != null)
        {
            StopCoroutine(setDefaultImageCoroutine);
            setDefaultImageCoroutine = null;
        }

        if (loadingPhotoEffect != null)
            loadingPhotoEffect.gameObject.SetActive(false);
    }

    #endregion


    #region CHARACTER DATA METHODS

    public void SetChatItemCharacterData(CharacterDataSO data)
    {
        characterData = data;

        if (characterData == null)
        {
            Debug.LogWarning("[ChatItemHome] CharacterDataSO is null.", this);

            if (setDefaultImageCoroutine != null)
            {
                StopCoroutine(setDefaultImageCoroutine);
                setDefaultImageCoroutine = null;
            }

            if (defaultPhoto != null)
                defaultPhoto.sprite = null;

            if (loadingPhotoEffect != null)
                loadingPhotoEffect.gameObject.SetActive(false);

            return;
        }

        characterDataIndex = GetCharacterDataIndex();

        SetChatItemState();
        SetChatItemMessageCount();
        SetChatItemCharacterInfo();
        SetChatItemCharacterRelationship();
        SetupChatItemLockState();
        SetChatItemDefaultImage();
    }

    private int GetCharacterDataIndex()
    {
        if (characterData == null ||
            CharacterDataManager.Instance == null ||
            CharacterDataManager.Instance.characterDatas == null)
        {
            return -1;
        }

        CharacterDataSO[] characterDatas = CharacterDataManager.Instance.characterDatas;

        for (int i = 0; i < characterDatas.Length; i++)
        {
            if (characterDatas[i] == characterData)
                return i;
        }

        return -1;
    }

    #endregion


    #region CHAT ITEM STATE METHODS

    public void SetChatItemState()
    {
        if (characterData == null)
            return;

        if (characterDataIndex < 0)
            characterDataIndex = GetCharacterDataIndex();

        bool isNew = characterDataIndex >= 0 && characterDataIndex < NEW_CHARACTER_COUNT;

        if (stateBackgroundImage != null)
        {
            stateBackgroundImage.color = isNew ? stateNewColor : stateHotColor;
        }

        if (stateText != null)
            stateText.text = isNew ? "New" : "Hot";
    }

    #endregion


    #region MESSAGE COUNT METHODS

    public void SetChatItemMessageCount()
    {
        if (messageCountText == null)
            return;

        if (characterData == null)
        {
            messageCountText.text = "0K";
            return;
        }

        messageCountText.text = characterData.messageCount;
    }

    #endregion


    #region DEFAULT PHOTO METHODS

    public void SetChatItemDefaultImage()
    {
        // Stop previous photo-check coroutine.
        if (setDefaultImageCoroutine != null)
        {
            StopCoroutine(setDefaultImageCoroutine);
            setDefaultImageCoroutine = null;
        }

        if (defaultPhoto == null)
            return;

        // Clear previous character's photo.
        defaultPhoto.sprite = null;

        if (characterData == null)
        {
            if (loadingPhotoEffect != null)
                loadingPhotoEffect.gameObject.SetActive(false);

            return;
        }

        // Show loading effect until photo 0 is available.
        if (loadingPhotoEffect != null)
            loadingPhotoEffect.gameObject.SetActive(true);

        setDefaultImageCoroutine = StartCoroutine(SetChatItemDefaultImageCrt());
    }

    private IEnumerator SetChatItemDefaultImageCrt()
    {
        CharacterDataSO currentCharacter = characterData;

        while (currentCharacter != null &&
               currentCharacter == characterData)
        {
            Sprite photo = null;

            // CharacterPhotoManager is responsible for loading photo 0.
            // ChatItemHome only reads the resulting sprite.
            if (currentCharacter.photoSprites != null &&
                currentCharacter.photoSprites.Length > 0)
            {
                photo = currentCharacter.photoSprites[0];
            }

            if (photo != null)
            {
                if (defaultPhoto != null)
                    defaultPhoto.sprite = photo;

                if (loadingPhotoEffect != null)
                    loadingPhotoEffect.gameObject.SetActive(false);

                setDefaultImageCoroutine = null;
                yield break;
            }

            // Photo is not ready: wait 1 second and check again.
            yield return new WaitForSecondsRealtime(PHOTO_CHECK_INTERVAL);
        }

        setDefaultImageCoroutine = null;
    }

    #endregion


    #region CHARACTER INFO METHODS

    public void SetChatItemCharacterInfo()
    {
        if (characterData == null || characterInfoText == null)
            return;

        characterInfoText.text =$"{characterData.characterName}, {characterData.age}";
    }

    #endregion


    #region CHARACTER RELATIONSHIP METHODS

    public void SetChatItemCharacterRelationship()
    {
        if (characterData == null || characterRelationshipGoalText == null)
        {
            return;
        }

        characterRelationshipGoalText.text = characterData.GetRelationshipGoalString();
    }

    #endregion


    #region LOCK STATE METHODS

    public void SetupChatItemLockState()
    {
        if (lockState == null)
            return;

        if (characterDataIndex < 0)
            characterDataIndex = GetCharacterDataIndex();

        bool isLocked = false;

        if (CharacterDataManager.Instance != null && characterDataIndex >= 0)
        {
            isLocked = CharacterDataManager.Instance.IsCharacterLocked(characterDataIndex);
        }

        lockState.gameObject.SetActive(isLocked);
    }

    #endregion


    #region BUTTON METHODS

    public void OnChatItemClick()
    {
        // Open Character Profile Panel.
        // You will implement this later.
    }

    #endregion
}
