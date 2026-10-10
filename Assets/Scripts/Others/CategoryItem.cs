using UnityEngine;
using UnityEngine.UI;

public class CategoryItem : MonoBehaviour
{
    #region VARIABLES

    [SerializeField]
    private Image background;

    [SerializeField]
    private Sprite normalBackground;

    [SerializeField]
    private Sprite choosingBackground;

    public bool isChosen;

    #endregion


    #region UNITY METHODS

    private void Start()
    {
        ChangeState();
    }

    #endregion


    #region CATEGORY METHODS

    public void ChangeState()
    {
        if (background == null)
        {
            Debug.LogWarning("[CategoryItem] Background Image is null.", this);
            return;
        }

        background.sprite = isChosen ? choosingBackground : normalBackground;
    }

    #endregion
}
