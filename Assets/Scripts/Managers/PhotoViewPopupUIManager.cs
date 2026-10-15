using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PhotoViewPopupUIManager : PanelBaseUIController
{
    #region VARIABLES

    public static PhotoViewPopupUIManager Instance;

    [Header("--- PHOTO DATA ---")]
    [SerializeField] private CharacterDataSO characterDataSO;
    [SerializeField] private int currentPhotoId = -1;

    [Header("--- UI ---")]
    [SerializeField] private Image photoImage;
    [SerializeField] private Button downloadPhotoButton;

    #endregion


    #region UNITY METHODS

    protected override void Awake()
    {
        base.Awake();

        Instance = this;
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
        RefreshPhotoUI();
    }

    public override void ShowDelay()
    {
        base.ShowDelay();
        RefreshPhotoUI();
    }

    public override void Hide()
    {
        base.Hide();
    }

    #endregion


    #region PHOTO METHODS

    public void ShowPhoto(CharacterDataSO character, int photoId)
    {
        if (character == null || photoId < 0 || photoId >= character.numberOfPhoto)
        {
            Debug.LogWarning("[PhotoView] Invalid photo data.");
            return;
        }

        characterDataSO = character;
        currentPhotoId = photoId;

        Show();
    }

    public void RefreshPhotoUI()
    {
        if (characterDataSO == null ||
            currentPhotoId < 0 ||
            currentPhotoId >= characterDataSO.numberOfPhoto)
        {
            if (photoImage != null)
                photoImage.enabled = false;

            if (downloadPhotoButton != null)
                downloadPhotoButton.gameObject.SetActive(false);

            return;
        }

        characterDataSO.SetListPhotoNumberOfElement();

        if (characterDataSO.IsPhotoDownloaded(currentPhotoId))
        {
            characterDataSO.LoadSavedPhotoToSprite(currentPhotoId);
        }

        Sprite sprite = null;

        if (characterDataSO.photoSprites != null &&
            currentPhotoId < characterDataSO.photoSprites.Length)
        {
            sprite = characterDataSO.photoSprites[currentPhotoId];
        }

        bool hasPhoto = sprite != null;

        if (photoImage != null)
        {
            photoImage.sprite = sprite;
            photoImage.enabled = hasPhoto;
            photoImage.preserveAspect = true;
        }

        if (downloadPhotoButton != null)
            downloadPhotoButton.gameObject.SetActive(!hasPhoto);
    }

    public void OnDownloadPhotoButtonClick()
    {
        /*
        if (characterDataSO == null || currentPhotoId < 0)
            return;

        if (characterDataSO.photoSprites == null ||
            currentPhotoId >= characterDataSO.photoSprites.Length)
            return;

        Sprite sprite = characterDataSO.photoSprites[currentPhotoId];

        if (sprite == null)
        {
            Debug.LogWarning("[PhotoView] Photo is not available.");
            return;
        }

        NativeGallery.SaveImageToGallery(sprite.texture, "AI Girlfriend", $"{characterDataSO.characterName}_{currentPhotoId}.png",
            (success, path) =>
            {
                if (success)
                    Debug.Log("[PhotoView] Photo saved to Gallery: " + path);
                else
                    Debug.LogWarning("[PhotoView] Failed to save photo.");
            }
        );
        */
    }

    #endregion
}