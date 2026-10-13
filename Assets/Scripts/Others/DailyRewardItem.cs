using UnityEngine;
using UnityEngine.UI;

public class DailyRewardItem : MonoBehaviour
{
    #region VARIABLES

    [Header("--- BACKGROUND ---")]
    [SerializeField] Image rewardBackgroundImage;
    [SerializeField] Sprite todayRewardBackgroundSprite;
    [SerializeField] Sprite notTodayRewardBackgroundSprite;

    [Header("--- REWARD ---")]
    [SerializeField] private int amount;


    [Header("--- ROSE ICON ---")]
    [SerializeField] Image roseIconImage;
    [SerializeField] Sprite roseIconOnSprite;
    [SerializeField] Sprite roseIconOffSprite;

    #endregion


    #region PROPERTIES

    public int Amount
    {
        get
        {
            return amount;
        }
        set
        {
            amount = Mathf.Max(0, value);
        }
    }

    #endregion


    #region UNITY METHODS

    private void Awake()
    {
    }

    private void Start()
    {
    }

    #endregion


    #region BACKGROUND METHODS

    public void SetBackgroundState(bool isTodayReward)
    {
        if (rewardBackgroundImage == null)
            return;

        Sprite targetSprite = isTodayReward ? todayRewardBackgroundSprite : notTodayRewardBackgroundSprite;

        if (targetSprite != null)
        {
            rewardBackgroundImage.sprite = targetSprite;
            rewardBackgroundImage.enabled = true;
        }
    }

    #endregion


    #region ROSE ICON METHODS

    public void SetRoseIconState(bool isClaimed)
    {
        if (roseIconImage == null)
            return;

        Sprite targetSprite = isClaimed ? roseIconOffSprite : roseIconOnSprite;

        if (targetSprite != null)
        {
            roseIconImage.sprite = targetSprite;
            roseIconImage.enabled = true;
        }
    }


    #endregion
}
