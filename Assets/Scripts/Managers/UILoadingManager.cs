
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UILoadingManager : MonoBehaviour
{
    #region VARIABLES

    private MyScenesManager myScenesManager;

    [SerializeField] Image loadingBarValue;

    [Header("--- LOADING SETTINGS ---")]
    [SerializeField] float loadingDuration = 5f;
    [SerializeField] string nextSceneName = "App Main Scene";

    #endregion


    #region UNITY METHODS

    private void Start()
    {
        myScenesManager = MyScenesManager.Instance;

        if (loadingBarValue != null)
        {
            loadingBarValue.type = Image.Type.Filled;
            loadingBarValue.fillMethod = Image.FillMethod.Horizontal;
            loadingBarValue.fillOrigin = 0;
            loadingBarValue.fillAmount = 0f;
        }

        StartCoroutine(FillLoadingBar());
    }

    #endregion


    #region LOADING METHODS

    private IEnumerator FillLoadingBar()
    {
        float elapsedTime = 0f;
        float duration = Mathf.Max(0.01f, loadingDuration);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsedTime / duration);

            if (loadingBarValue != null)
                loadingBarValue.fillAmount = progress;

            yield return null;
        }

        if (loadingBarValue != null)
            loadingBarValue.fillAmount = 1f;

        yield return null;

        if (myScenesManager == null)
            myScenesManager = MyScenesManager.Instance;

        if (myScenesManager != null)
        {
            myScenesManager.ChangeScene(nextSceneName);
        }
        else
        {
            Debug.LogError("[UILoadingManager] MyScenesManager.Instance is null.");
        }
    }

    #endregion
}
