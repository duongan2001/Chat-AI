using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageConfig : MonoBehaviour
{
    #region VARIABLES

    [Header("====== CONFIG ======")]
    [SerializeField] bool isUserMessage;
    [SerializeField] float textHeightThreshold;
    [SerializeField] float textWidthThreshold;
    [SerializeField] RectTransform thisRect;

    [Header("====== CONTENT ======")]
    [SerializeField] TextMeshProUGUI contentText;

    #endregion

    #region UNITY METHODS

    private void Start()
    {
        thisRect = GetComponent<RectTransform>();
        StartCoroutine(SetBubbleStateCrt());
    }

    #endregion

    #region CONFIG METHODS

    IEnumerator SetBubbleStateCrt()
    {
        yield return null;
        SetBubbleState();
    }    

    void SetBubbleState()
    {
        RectTransform textRect = contentText.GetComponent<RectTransform>();
        LayoutElement textLayoutElement = contentText.GetComponent <LayoutElement>();

        if (textRect.rect.width >= textWidthThreshold)
        {
            textLayoutElement.enabled = true;
        }
        else
        {
            textLayoutElement.enabled = false;
        }
    }      

    public void SetContentText(string content)
    {
        contentText.text = content;
    }    

    #endregion
}
