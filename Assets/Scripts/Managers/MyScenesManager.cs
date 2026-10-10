using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MyScenesManager : MonoBehaviour
{
    #region VARIABLES

    public static MyScenesManager Instance;

    private Coroutine changeSceneCoroutine;
    private bool isChangingScene;

    #endregion


    #region UNITY METHODS

    private void Awake()
    {
        SetupInstance();
    }

    private void Start()
    {
    }

    #endregion


    #region SINGLETON METHODS

    private void SetupInstance()
    {
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);

            return;
        }

        if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    #endregion


    #region SCENE METHODS

    public void ChangeScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[MyScenesManager] Scene name is empty.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[MyScenesManager] Scene '{sceneName}' is not available in Build Settings.");
            return;
        }

        if (isChangingScene)
        {
            return;
        }

        if (changeSceneCoroutine != null)
        {
            StopCoroutine(changeSceneCoroutine);
            changeSceneCoroutine = null;
        }

        isChangingScene = true;

        Debug.Log($"[MyScenesManager] Loading scene: {sceneName}");

        SceneManager.LoadScene(sceneName);

        isChangingScene = false;
    }

    public void ChangeSceneDelay(string sceneName, float time)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[MyScenesManager] Scene name is empty.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[MyScenesManager] Scene '{sceneName}' is not available in Build Settings.");
            return;
        }

        if (isChangingScene)
        {
            return;
        }

        if (changeSceneCoroutine != null)
        {
            StopCoroutine(changeSceneCoroutine);
        }

        changeSceneCoroutine = StartCoroutine(ChangeSceneDelayCoroutine(sceneName, time));
    }

    private IEnumerator ChangeSceneDelayCoroutine(string sceneName, float time)
    {
        if (time > 0f)
        {
            yield return new WaitForSecondsRealtime(time);
        }

        changeSceneCoroutine = null;

        ChangeScene(sceneName);
    }

    #endregion
}
