using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UnlockPhotoPopupUIManager : PanelBaseUIController
{
    #region VARIABLES

    [Header("--- PHOTO DATA ---")]
    [SerializeField] private CharacterDataSO characterDataSO;
    [SerializeField] private int currentPhotoId = -1;

    [Header("--- ROSE SETTINGS ---")]
    [SerializeField] private int roseUnlockCost = 150;

    [Header("--- BUTTONS ---")]
    [SerializeField] private Button closePopupButton;
    [SerializeField] private Button unlockPhotoButton;

    [Header("--- TEXTS ---")]
    [SerializeField] private Text unlockText;
    [SerializeField] private Text downloadingText;

    private bool isDownloading;
    private Coroutine downloadCoroutine;

    #endregion


    #region UNITY METHODS

    protected override void Awake()
    {
        base.Awake();

        ResetPopupUI();
    }

    private void OnDestroy()
    {
        if (downloadCoroutine != null)
        {
            StopCoroutine(downloadCoroutine);
            downloadCoroutine = null;
        }
    }

    #endregion


    #region PANEL METHODS

    public override void Show()
    {
        if (!isDownloading)
            ResetPopupUI();

        base.Show();
    }

    public override void ShowDelay()
    {
        if (!isDownloading)
            ResetPopupUI();

        base.ShowDelay();
    }

    public override void Hide()
    {
        if (isDownloading)
            return;

        ResetPopupUI();
        base.Hide();
    }

    #endregion


    #region PHOTO METHODS

    public void SetPhotoData(CharacterDataSO characterData, int photoId)
    {
        if (isDownloading)
            return;

        characterDataSO = characterData;
        currentPhotoId = photoId;
    }

    public void OnUnlockPhotoButtonClick()
    {
        if (isDownloading)
            return;

        if (characterDataSO == null || currentPhotoId < 0)
        {
            Debug.LogWarning("[UnlockPhoto] Invalid photo data.");
            return;
        }

        if (characterDataSO.IsPhotoDownloaded(currentPhotoId))
        {
            Debug.Log("[UnlockPhoto] Photo already downloaded.");
            Hide();
            return;
        }

        int cost = Mathf.Max(0, roseUnlockCost);
        string roseKey = PlayerPrefKey.NumberOfRose.ToString();

        int currentRoses = PlayerPrefs.GetInt(roseKey, 0);

        // Not enough Roses.
        if (currentRoses < cost)
        {
            Hide();

            if (RoseNotiUIManager.Instance != null)
            {
                RoseNotiUIManager.Instance.ShowNotEnoughRosesPopup();
            }
            else
            {
                Debug.LogWarning("[UnlockPhoto] RoseNotiUIManager is null.");
            }

            return;
        }

        // Deduct Roses.
        if (cost > 0)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateRoseAmount(-cost);
            }
            else
            {
                PlayerPrefs.SetInt(roseKey, currentRoses - cost);
                PlayerPrefs.Save();
            }
        }

        isDownloading = true;
        SetDownloadingUI();

        downloadCoroutine = StartCoroutine(DownloadPhotoCoroutine());
    }

    private IEnumerator DownloadPhotoCoroutine()
    {
        int photoId = currentPhotoId;
        CharacterDataSO character = characterDataSO;
        int refundAmount = Mathf.Max(0, roseUnlockCost);

        bool isSuccess = false;

        if (character != null && photoId >= 0)
        {
            // Download the selected photo.
            yield return StartCoroutine(character.DownloadPhoto(photoId));

            // Verify the photo was saved successfully.
            isSuccess = character.IsPhotoDownloaded(photoId);

            if (isSuccess)
            {
                // Load the saved photo into Sprite if needed.
                character.LoadSavedPhotoToSprite(photoId);

                if (UIManager.Instance.characterProfilePanel != null)
                {
                    UIManager.Instance.characterProfilePanel.RefreshCharacterPhotoList();
                }

                if (PhotoViewPopupUIManager.Instance != null)
                {
                    PhotoViewPopupUIManager.Instance.RefreshPhotoUI();
                }

                Debug.Log($"[UnlockPhoto] Photo downloaded: {character.characterName}, ID: {photoId}");
            }
        }

        if (!isSuccess)
        {
            Debug.LogWarning("[UnlockPhoto] Photo download failed.");

            // Refund Roses if the download failed.
            if (refundAmount > 0)
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.UpdateRoseAmount(refundAmount);
                }
                else
                {
                    string key = PlayerPrefKey.NumberOfRose.ToString();
                    int current = PlayerPrefs.GetInt(key, 0);

                    long total = (long)current + refundAmount;

                    PlayerPrefs.SetInt(key, (int)Math.Min(total, int.MaxValue));
                    PlayerPrefs.Save();
                }
            }
        }

        isDownloading = false;
        downloadCoroutine = null;

        ResetPopupUI();
        Hide();
    }

    #endregion


    #region UI METHODS

    private void SetDownloadingUI()
    {
        if (closePopupButton != null)
            closePopupButton.gameObject.SetActive(false);

        if (unlockPhotoButton != null)
            unlockPhotoButton.interactable = false;

        if (unlockText != null)
            unlockText.gameObject.SetActive(false);

        if (downloadingText != null)
            downloadingText.gameObject.SetActive(true);
    }

    private void ResetPopupUI()
    {
        if (closePopupButton != null)
            closePopupButton.gameObject.SetActive(true);

        if (unlockPhotoButton != null)
            unlockPhotoButton.interactable = true;

        if (unlockText != null)
            unlockText.gameObject.SetActive(true);

        if (downloadingText != null)
            downloadingText.gameObject.SetActive(false);
    }

    #endregion
}