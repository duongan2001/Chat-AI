using UnityEngine;

public class ShopPanelUIManager : PanelBaseUIController
{
    #region VARIABLES

    [Header("--- ROSE PURCHASE EFFECT ---")]
    [SerializeField] private RectTransform[] rosePackagePoints = new RectTransform[6];
    [SerializeField] private RectTransform roseBalancePoint;

    #endregion


    #region UNITY METHODS

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
    }

    #endregion


    #region PANEL METHODS

    public override void Show()
    {
        base.Show();

        RefreshShopUI();
    }

    public override void Hide()
    {
        base.Hide();
    }

    public override void ShowDelay()
    {
        base.ShowDelay();

        RefreshShopUI();
    }

    #endregion


    #region SHOP METHODS

    private void RefreshShopUI()
    {
        // Update current Rose amount.
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateRoseAmountUI();
        }

        // Update localized IAP prices.
        IAPPuchase[] purchaseItems = GetComponentsInChildren<IAPPuchase>(true);

        foreach (IAPPuchase item in purchaseItems)
        {
            if (item != null)
                item.CheckProduct();
        }
    }

    public void PlayRosePurchaseEffect(int id)
    {
        if (id < 0 || id >= rosePackagePoints.Length)
        {
            Debug.LogWarning($"[Shop] Invalid Rose package ID: {id}");
            return;
        }

        RectTransform startPoint = rosePackagePoints[id];

        if (startPoint == null || roseBalancePoint == null)
        {
            Debug.LogWarning("[Shop] Rose effect points are not assigned.");
            return;
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.PlayRoseBurstEffect(startPoint, roseBalancePoint);
        }
    }

    #endregion
}
