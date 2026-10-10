
using TMPro;
using UnityEngine;

public class MessageUIConfig : MonoBehaviour
{
    #region VARIABLES

    [Header("====== CONFIG ======")]
    [SerializeField] RectTransform thisRect;

    [Header("====== CONTENT ======")]
    [SerializeField] TextMeshProUGUI contentText;

    #endregion


    #region UNITY METHODS

    private void Start()
    {
        if (thisRect == null)
            thisRect = GetComponent<RectTransform>();

        if (contentText == null)
            contentText = GetComponentInChildren<TextMeshProUGUI>();
    }

    #endregion


    #region MESSAGE METHODS

    public void SetContentText(string content)
    {
        if (contentText == null)
            contentText = GetComponentInChildren<TextMeshProUGUI>();

        if (contentText == null)
            return;

        contentText.text = content ?? string.Empty;
    }

    #endregion
}
