using UnityEngine;
using UnityEngine.UI;

public class CharaterPhoto : MonoBehaviour
{
    #region VARIABLES

    private CharacterDataSO characterDataSO;

    [Header("--- PHOTO DATA ---")]
    [SerializeField] private int photoIdInList;
    public int PhotoIdInList => photoIdInList;

    [Header("--- UI ---")]
    [SerializeField] private Image characterPhoto;
    [SerializeField] private Image lockCensorEffect;

    #endregion


    #region CHARACTER DATA METHODS

    public void SetCharacterData(CharacterDataSO data)
    {
        characterDataSO = data;
    }

    public void SetPhotoId(int id)
    {
        photoIdInList = id;
    }

    #endregion


    #region PHOTO METHODS

    public void SetPhoto()
    {
        if (characterDataSO == null ||
            photoIdInList < 0 ||
            photoIdInList >= characterDataSO.numberOfPhoto)
        {
            SetPhotoLocked();
            return;
        }

        characterDataSO.SetListPhotoNumberOfElement();

        // Check if the photo has already been downloaded.
        bool isDownloaded = characterDataSO.IsPhotoDownloaded(photoIdInList);

        if (!isDownloaded)
        {
            SetPhotoLocked();
            return;
        }

        // Load saved photo into Sprite.
        if (characterDataSO.photoSprites[photoIdInList] == null)
        {
            characterDataSO.LoadSavedPhotoToSprite(photoIdInList);
        }

        Sprite sprite = characterDataSO.photoSprites[photoIdInList];

        if (sprite == null)
        {
            SetPhotoLocked();
            return;
        }

        // Show unlocked photo.
        if (characterPhoto != null)
        {
            characterPhoto.sprite = sprite;
            characterPhoto.enabled = true;
            characterPhoto.preserveAspect = true;
        }

        if (lockCensorEffect != null)
        {
            lockCensorEffect.gameObject.SetActive(false);
        }
    }

    private void SetPhotoLocked()
    {
        if (characterPhoto != null)
        {
            characterPhoto.sprite = null;
            characterPhoto.enabled = false;
        }

        if (lockCensorEffect != null)
        {
            lockCensorEffect.gameObject.SetActive(true);
        }
    }

    #endregion


    #region BUTTON METHODS

    public void OnPhotoClick()
    {
        SoundManager.Instance.PlaySFX(1);

        if (characterDataSO == null)
            return;

        // Prevent viewing locked photos.
        if (!characterDataSO.IsPhotoDownloaded(photoIdInList))
        {
            OnCensorEffectClick();
            return;
        }

        if (UIManager.Instance.photoViewPopup == null)
        {
            Debug.LogWarning("[CharaterPhoto] PhotoViewPopup is null.");
            return;
        }

        UIManager.Instance.photoViewPopup.ShowPhoto(characterDataSO, photoIdInList);
    }

    public void OnCensorEffectClick()
    {
        SoundManager.Instance.PlaySFX(1);

        if (characterDataSO == null)
            return;

        // Already unlocked.
        if (characterDataSO.IsPhotoDownloaded(photoIdInList))
        {
            OnPhotoClick();
            return;
        }

        if (UIManager.Instance.unlockPhotoPopup == null)
        {
            Debug.LogWarning("[CharaterPhoto] UnlockPhotoPopup is null.");
            return;
        }

        UIManager.Instance.unlockPhotoPopup.SetPhotoData(characterDataSO, photoIdInList);
        UIManager.Instance.unlockPhotoPopup.Show();
    }

    #endregion
}