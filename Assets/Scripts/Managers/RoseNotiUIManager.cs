using UnityEngine;

public class RoseNotiUIManager : PanelBaseUIController
{
    #region VARIABLES

    public static RoseNotiUIManager Instance;

    [Header("--- POPUPS ---")]
    [SerializeField] private RectTransform noMessagesAvailablePopup;
    [SerializeField] private RectTransform notEnoughRosesPopup;

    [Header("--- ROSE SETTINGS ---")]
    [SerializeField] private int roseCostPerMessage = 5;

    #endregion


    #region UNITY METHODS

    protected override void Awake()
    {
        base.Awake();

        Instance = this;
    }

    private void Start()
    {
        HideAllPopups();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    #endregion


    #region POPUP METHODS

    private void HideAllPopups()
    {
        if (noMessagesAvailablePopup != null)
            noMessagesAvailablePopup.gameObject.SetActive(false);

        if (notEnoughRosesPopup != null)
            notEnoughRosesPopup.gameObject.SetActive(false);
    }

    public void ShowNoMessageAvailablePopup()
    {
        HideAllPopups();

        Show();

        if (noMessagesAvailablePopup != null)
            noMessagesAvailablePopup.gameObject.SetActive(true);
    }

    public void ShowNotEnoughRosesPopup()
    {
        HideAllPopups();

        Show();

        if (notEnoughRosesPopup != null)
            notEnoughRosesPopup.gameObject.SetActive(true);
    }

    #endregion


    #region ROSE METHODS

    public void AcceptUseRoseForMessage()
    {
        string roseKey = PlayerPrefKey.NumberOfRose.ToString();
        string chatKey = PlayerPrefKey.DailyChatRemain.ToString();

        int currentRoses = PlayerPrefs.GetInt(roseKey, 0);
        int cost = Mathf.Max(1, roseCostPerMessage);

        if (currentRoses < cost)
        {
            ShowNotEnoughRosesPopup();
            return;
        }

        // Consume Roses.
        PlayerPrefs.SetInt(roseKey, currentRoses - cost);

        // Grant one chat message.
        int remainingChats = PlayerPrefs.GetInt(chatKey, 0);
        PlayerPrefs.SetInt(chatKey, remainingChats + 1);

        PlayerPrefs.Save();

        // Update the Rose amount in the main UI.
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateRoseAmount(0);

        Hide();
    }

    #endregion


    #region SHOP METHODS

    public void GoToShop()
    {
        Hide();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.roseShopPanel.Show();
        }
        else
        {
            Debug.LogWarning("[RoseNoti] Shop Panel is not assigned.");
        }
    }

    #endregion


    #region PANEL METHODS

    public override void Show()
    {
        base.Show();
    }

    public override void ShowDelay()
    {
        base.ShowDelay();
    }

    public override void Hide()
    {
        HideAllPopups();
        base.Hide();
    }

    #endregion
}