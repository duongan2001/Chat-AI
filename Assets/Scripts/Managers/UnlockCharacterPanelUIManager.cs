using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnlockCharacterPanelUIManager : PanelBaseUIController
{
    #region VARIABLES

    [Header("--- CHARACTER DATA SCRIPTABLE ---")]
    [SerializeField] private CharacterDataSO characterData;

    private int currentCharId = -1;
    private int charWillBeShowId = -1;

    [Header("--- BACKGROUND IMAGE ---")]
    [SerializeField] private Image backgroundImage;

    private Coroutine setImagesCoroutine;

    [Header("--- ANIMATION TIME ---")]
    [SerializeField] private float animTimeScale = 1f;

    [Header("--- UNLOCK ---")]
    [SerializeField] private Image title;
    [SerializeField] private Button closeButton;
    [SerializeField] private Image lockStateBackground;
    [SerializeField] private Image lockIcon;
    [SerializeField] private Text price;
    [SerializeField] private Image roseIcon;
    [SerializeField] private Button unlockButton;

    [Header("--- UNLOCK SETTINGS ---")]
    [SerializeField] private int unlockPrice = 100;

    private Sequence unlockSequence;
    private bool isUnlocking;

    private Graphic[] quickFadeGraphics;
    private float[] quickFadeOriginalAlpha;

    private float lockBgOriginalAlpha;
    private float lockIconOriginalAlpha;
    private Vector3 lockIconOriginalScale;

    private bool needReset;

    private const float PHOTO_CHECK_INTERVAL = 1f;

    #endregion


    #region UNITY METHODS

    private void Start()
    {
        CacheOriginalState();
        UpdatePriceUI();

        if (unlockButton != null)
        {
            unlockButton.onClick.RemoveListener(OnUnlockButtonClick);
            unlockButton.onClick.AddListener(OnUnlockButtonClick);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }
    }

    private void OnDisable()
    {
        StopImageCoroutine();

        if (unlockSequence != null)
        {
            unlockSequence.Kill();
            unlockSequence = null;
        }

        ResetIfNeeded();
        isUnlocking = false;
    }

    #endregion


    #region PANEL METHODS

    public override void Show()
    {
        ResetUnlockUI();

        base.Show();

        SetCharacterImages();
        UpdatePriceUI();

        if (closeButton != null)
            closeButton.interactable = true;

        if (unlockButton != null)
            unlockButton.interactable = true;
    }

    public override void ShowDelay()
    {
        ResetUnlockUI();

        base.ShowDelay();

        SetCharacterImages();
        UpdatePriceUI();
    }

    public override void Hide()
    {
        if (isUnlocking)
            return;

        StopImageCoroutine();
        ResetIfNeeded();

        base.Hide();
    }

    #endregion


    #region CHARACTER DATA METHODS

    public void SetCharacterData(CharacterDataSO data)
    {
        StopImageCoroutine();

        characterData = data;

        currentCharId = GetCharacterDataIndex(data);
        charWillBeShowId = currentCharId;

        if (backgroundImage != null)
        {
            backgroundImage.sprite = null;
            backgroundImage.enabled = false;
        }

        SetCharacterImages();
        UpdatePriceUI();
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


    #region CHARACTER IMAGE METHODS

    public void SetCharacterImages()
    {
        StopImageCoroutine();

        if (characterData == null)
            return;

        setImagesCoroutine = StartCoroutine(SetCharacterDefaultImagesCrt());
    }

    private IEnumerator SetCharacterDefaultImagesCrt()
    {
        CharacterDataSO targetCharacter = characterData;

        while (targetCharacter != null)
        {
            if (targetCharacter != characterData)
                yield break;

            Sprite sprite = null;

            if (targetCharacter.photoSprites != null &&
                targetCharacter.photoSprites.Length > 0)
            {
                sprite = targetCharacter.photoSprites[0];
            }

            if (sprite != null)
            {
                if (backgroundImage != null)
                {
                    backgroundImage.sprite = sprite;
                    backgroundImage.enabled = true;
                }

                setImagesCoroutine = null;
                yield break;
            }

            yield return new WaitForSecondsRealtime(PHOTO_CHECK_INTERVAL);
        }

        setImagesCoroutine = null;
    }

    private void StopImageCoroutine()
    {
        if (setImagesCoroutine == null)
            return;

        StopCoroutine(setImagesCoroutine);
        setImagesCoroutine = null;
    }

    #endregion


    #region UNLOCK METHODS

    public void OnUnlockButtonClick()
    {
        if (isUnlocking || characterData == null)
            return;

        if (CharacterDataManager.Instance == null ||
            UIManager.Instance == null ||
            charWillBeShowId < 0)
        {
            Debug.LogWarning("[UnlockCharacterPanel] Missing required manager.");
            return;
        }

        if (!CharacterDataManager.Instance.IsCharacterLocked(charWillBeShowId))
        {
            Debug.Log("[UnlockCharacterPanel] Character already unlocked.");

            Hide();
            return;
        }

        int currentRoses = PlayerPrefs.GetInt(PlayerPrefKey.NumberOfRose.ToString(), 0);

        int cost = Mathf.Max(0, unlockPrice);

        if (currentRoses < cost)
        {
            Debug.Log("[UnlockCharacterPanel] Not enough Roses.");

            // You can open the Rose Shop here.
            return;
        }

        // Deduct Rose and unlock character.
        UIManager.Instance.UpdateRoseAmount(-cost);

        CharacterDataManager.Instance.UnlockCharacter(charWillBeShowId);

        PlayUnlockAnimation();
    }

    private void PlayUnlockAnimation()
    {
        if (isUnlocking)
            return;

        isUnlocking = true;
        needReset = true;

        if (unlockButton != null)
            unlockButton.interactable = false;

        if (closeButton != null)
            closeButton.interactable = false;

        if (unlockSequence != null)
            unlockSequence.Kill();

        float durationScale = Mathf.Max(0.01f, animTimeScale);

        unlockSequence = DOTween.Sequence().SetUpdate(true);

        // Fade out the lock background.
        if (lockStateBackground != null)
        {
            unlockSequence.Join(lockStateBackground.DOFade(0f, 0.5f * durationScale));
        }

        // Lock icon scales up and fades out.
        if (lockIcon != null)
        {
            unlockSequence.Join(lockIcon.transform.DOScale(lockIconOriginalScale * 1.5f, 0.5f * durationScale));
            unlockSequence.Join(lockIcon.DOFade(0f, 0.5f * durationScale));
        }

        // Fade other UI elements.
        if (quickFadeGraphics != null)
        {
            foreach (Graphic graphic in quickFadeGraphics)
            {
                if (graphic == null)
                    continue;

                unlockSequence.Join(graphic.DOFade(0f, 0.5f * durationScale));
            }
        }

        unlockSequence.AppendInterval(0.5f * durationScale);
        unlockSequence.OnComplete(HideAfterUnlockCharacter);
    }

    private void HideAfterUnlockCharacter()
    {
        isUnlocking = false;

        if (UIManager.Instance != null &&
            UIManager.Instance.homePanel != null)
        {
            UIManager.Instance.homePanel.RefreshAllChatItems();
        }

        needReset = true;
        base.Hide();
    }

    #endregion


    #region ANIMATION STATE METHODS

    private void CacheOriginalState()
    {
        if (lockStateBackground != null)
            lockBgOriginalAlpha = lockStateBackground.color.a;

        if (lockIcon != null)
        {
            lockIconOriginalAlpha = lockIcon.color.a;
            lockIconOriginalScale = lockIcon.transform.localScale;
        }

        List<Graphic> graphics = new List<Graphic>();

        if (title != null)
            graphics.Add(title);

        if (price != null)
            graphics.Add(price);

        if (roseIcon != null)
            graphics.Add(roseIcon);

        quickFadeGraphics = graphics.ToArray();

        quickFadeOriginalAlpha = new float[quickFadeGraphics.Length];

        for (int i = 0; i < quickFadeGraphics.Length; i++)
        {
            quickFadeOriginalAlpha[i] = quickFadeGraphics[i] != null ? quickFadeGraphics[i].color.a : 1f;
        }
    }

    private void ResetUnlockUI()
    {
        if (unlockSequence != null)
        {
            unlockSequence.Kill();
            unlockSequence = null;
        }

        if (lockStateBackground != null)
            SetAlpha(lockStateBackground, lockBgOriginalAlpha);

        if (lockIcon != null)
        {
            SetAlpha(lockIcon, lockIconOriginalAlpha);

            lockIcon.transform.localScale = lockIconOriginalScale;
        }

        if (quickFadeGraphics != null)
        {
            for (int i = 0; i < quickFadeGraphics.Length; i++)
            {
                if (quickFadeGraphics[i] == null)
                    continue;

                SetAlpha(quickFadeGraphics[i], quickFadeOriginalAlpha[i]);
            }
        }

        if (unlockButton != null)
            unlockButton.interactable = true;

        if (closeButton != null)
            closeButton.interactable = true;

        isUnlocking = false;
        needReset = false;
    }

    private static void SetAlpha(Graphic g, float a)
    {
        if (g == null)
            return;

        Color color = g.color;
        color.a = Mathf.Clamp01(a);
        g.color = color;
    }

    private void ResetIfNeeded()
    {
        if (needReset)
            ResetUnlockUI();
    }

    #endregion


    #region UI METHODS

    private void UpdatePriceUI()
    {
        if (price != null)
            price.text = Mathf.Max(0, unlockPrice).ToString();
    }

    #endregion


    #region TEST METHODS

    [ContextMenu("Test Unlock Animation")]
    private void TestUnlockAnimation()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[UnlockCharacterPanel] Test in Play Mode.");
            return;
        }

        ResetIfNeeded();
        PlayUnlockAnimation();
    }

    #endregion
}
