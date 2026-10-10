using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class CharacterPhotoManager : MonoBehaviour
{
    #region VARIABLES

    public static CharacterPhotoManager Instance;

    [SerializeField]
    [Header("--- CHARACTER DATA SCRIPTABLE OBJECTS ---")]
    private CharacterDataSO[] characterDatas;

    [Header("--- PHOTO JSON CONFIG ---")]
    [SerializeField]
    private string characterPhotoJsonUrl;

    private const string CACHE_FILE_NAME = "character_photos.json";

    private Dictionary<string, string[]> photoUrlDictionary;

    private string CacheFilePath => Path.Combine(Application.persistentDataPath, CACHE_FILE_NAME);

    #endregion


    #region UNITY METHODS

    private void Awake()
    {
        SetupInstance();
    }

    private void Start()
    {
        if (Instance != this)
        {
            return;
        }

        if ((characterDatas == null || characterDatas.Length == 0) &&
            CharacterDataManager.Instance != null)
        {
            characterDatas = CharacterDataManager.Instance.characterDatas;
        }

        StartCoroutine(LoadPhotoUrlsFromJson());
    }

    #endregion


    #region SINGLETON METHODS

    private void SetupInstance()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }

        if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    #endregion


    #region JSON METHODS

    private IEnumerator LoadPhotoUrlsFromJson()
    {
        bool loadedSuccessfully = false;

        // Try downloading the latest JSON from the server.
        if (!string.IsNullOrWhiteSpace(characterPhotoJsonUrl))
        {
            using (UnityWebRequest request = UnityWebRequest.Get(characterPhotoJsonUrl))
            {
                request.timeout = 20;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string json = request.downloadHandler.text;

                    if (CheckPhotoUrlChanges(json) && TryApplyPhotoJson(json))
                    {
                        loadedSuccessfully = true;

                        SaveJsonCache(json);

                        Debug.Log("[CharacterPhotoManager] Online JSON loaded successfully.");
                    }
                }
                else
                {
                    Debug.LogWarning($"[CharacterPhotoManager] Download JSON failed: {request.error}");
                }
            }
        }

        // Fallback to cached JSON when offline or download fails.
        if (!loadedSuccessfully)
        {
            if (File.Exists(CacheFilePath))
            {
                try
                {
                    string cachedJson = File.ReadAllText(CacheFilePath);

                    loadedSuccessfully = TryApplyPhotoJson(cachedJson);

                    if (loadedSuccessfully)
                    {
                        Debug.Log("[CharacterPhotoManager] Loaded cached photo JSON.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CharacterPhotoManager] Cache load failed: {e.Message}");
                }
            }
        }

        if (!loadedSuccessfully)
        {
            Debug.LogWarning("[CharacterPhotoManager] No valid photo JSON available.");

            yield break;
        }

        // Load default photos after photo URLs are initialized.
        yield return StartCoroutine(SetupDefaultPhotos());
    }

    private bool TryApplyPhotoJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            Dictionary<string, string[]> parsedData = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(json);

            if (parsedData == null || parsedData.Count == 0)
            {
                return false;
            }

            if (characterDatas == null || characterDatas.Length == 0)
            {
                Debug.LogWarning("[CharacterPhotoManager] Character data array is empty.");

                return false;
            }

            photoUrlDictionary = parsedData;

            foreach (CharacterDataSO character in characterDatas)
            {
                if (character == null)
                {
                    continue;
                }

                if (photoUrlDictionary.TryGetValue(character.characterName, out string[] urls))
                {
                    character.photoUrls = urls ?? Array.Empty<string>();

                    character.numberOfPhoto = character.photoUrls.Length;

                    character.SetListPhotoNumberOfElement();
                }
                else
                {
                    Debug.LogWarning($"[CharacterPhotoManager] No photo URLs found for {character.characterName}");
                }
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[CharacterPhotoManager] JSON parse failed: {e.Message}");

            return false;
        }
    }



    private bool CheckPhotoUrlChanges(string newJson)
    {
        if (!File.Exists(CacheFilePath))
        {
            return true;
        }

        try
        {
            string oldJson = File.ReadAllText(CacheFilePath);

            Dictionary<string, string[]> oldData = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(oldJson);

            Dictionary<string, string[]> newData = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(newJson);

            if (oldData == null || newData == null || characterDatas == null)
            {
                return false;
            }

            foreach (CharacterDataSO character in characterDatas)
            {
                if (character == null)
                    continue;

                if (!newData.TryGetValue(character.characterName, out string[] newUrls))
                    continue;

                oldData.TryGetValue(character.characterName, out string[] oldUrls);

                if (newUrls == null)
                    continue;

                for (int i = 0; i < newUrls.Length; i++)
                {
                    string oldUrl = oldUrls != null && i < oldUrls.Length ? oldUrls[i] : null;
                    
                    if (oldUrl == newUrls[i])
                        continue;

                    // Delete cached image and clear loaded Sprite.
                    character.DeleteCachedPhoto(i);

                    Debug.Log($"[CharacterPhotoManager] Photo URL changed: {character.characterName}, ID {i}");
                }
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[CharacterPhotoManager] Photo URL comparison failed: {e.Message}");

            return false;
        }
    }



    private void SaveJsonCache(string json)
    {
        try
        {
            File.WriteAllText(CacheFilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CharacterPhotoManager] Cache save failed: {e.Message}");
        }
    }

    #endregion


    #region CHARACTER PHOTO METHODS

    private IEnumerator SetupDefaultPhotos()
    {
        if (characterDatas == null)
        {
            yield break;
        }

        for (int i = 0; i < characterDatas.Length; i++)
        {
            CharacterDataSO character = characterDatas[i];

            if (character == null || character.numberOfPhoto <= 0)
            {
                continue;
            }

            character.SetListPhotoNumberOfElement();

            if (character.photoSprites[0] != null)
            {
                continue;
            }

            if (character.IsPhotoDownloaded(0))
            {
                character.LoadSavedPhotoToSprite(0);
            }
            else
            {
                yield return StartCoroutine(character.DownloadPhoto(0));
            }
        }
    }

    public void SetChatItemDefaultPhoto(int id)
    {
        if (characterDatas == null ||
            id < 0 ||
            id >= characterDatas.Length)
        {
            return;
        }

        CharacterDataSO character = characterDatas[id];

        if (character == null || character.numberOfPhoto <= 0)
        {
            return;
        }

        character.SetListPhotoNumberOfElement();

        if (character.photoSprites[0] != null)
        {
            return;
        }

        if (character.IsPhotoDownloaded(0))
        {
            character.LoadSavedPhotoToSprite(0);
        }
        else
        {
            StartCoroutine(character.DownloadPhoto(0));
        }
    }

    private IEnumerator DownloadDefaultPhoto(CharacterDataSO character)
    {
        yield return StartCoroutine(character.DownloadPhoto(0));
    }

    #endregion
}
