using UnityEngine;

public class ShopPanelUIManager : PanelBaseUIController
{
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

    #endregion
}
