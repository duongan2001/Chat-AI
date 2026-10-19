using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomChatNotiPopupUIManager : PanelBaseUIController
{
    //Panel show, hide

    public void OnBecomeVipButtonClick()
    {
        Hide();
        UIManager.Instance.subPanel.ShowDelay();
    }    
}
