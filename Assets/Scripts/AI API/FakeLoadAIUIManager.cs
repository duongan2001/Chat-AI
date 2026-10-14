using System.Collections;
using UnityEngine;

public class FakeLoadAIUIManager : PanelBaseUIController
{
    #region VARIABLES

    [Header("--- FAKE LOADING SETTINGS ---")]
    [SerializeField] private float loadingDuration = 5f;

    private Coroutine delayHideCoroutine;

    #endregion


    #region UNITY METHODS

    private void Start()
    {
    }

    private void OnDisable()
    {
        StopDelayHideCoroutine();
    }

    #endregion


    #region PANEL METHODS

    public override void Show()
    {
        StopDelayHideCoroutine();

        base.Show();

        delayHideCoroutine = StartCoroutine(DelayHideCoroutine());
    }

    public override void ShowDelay()
    {
        StopDelayHideCoroutine();

        base.ShowDelay();

        delayHideCoroutine = StartCoroutine(DelayHideCoroutine());
    }

    public override void Hide()
    {
        StopDelayHideCoroutine();

        base.Hide();
    }

    #endregion


    #region LOADING METHODS

    public void DelayHidePanel()
    {
        StopDelayHideCoroutine();

        delayHideCoroutine = StartCoroutine(DelayHideCoroutine());
    }

    private IEnumerator DelayHideCoroutine()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, loadingDuration));

        delayHideCoroutine = null;

        Hide();

        if (UIManager.Instance != null)
            UIManager.Instance.OnFakeAILoadingCompleted();
    }

    private void StopDelayHideCoroutine()
    {
        if (delayHideCoroutine != null)
        {
            StopCoroutine(delayHideCoroutine);
            delayHideCoroutine = null;
        }
    }

    #endregion
}
