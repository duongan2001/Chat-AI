using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class CharacterDataManager : MonoBehaviour
{
    #region VARIABLES

    public static CharacterDataManager Instance;

    [Header("--- CHARACTER DATA SCRIPTABLE OBJECTS ---")]
    public CharacterDataSO[] characterDatas;

    [Header("--- CHARACTER LOCK STATE ---")]
    private const string SAVE_FILE_NAME = "character_lock_state.json";

    [SerializeField]
    private CharacterLockStateData saveLockData;

    public int[] defaultUnlockedIndexes;

    private string SavePath
    {
        get
        {
            return Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        }
    }

    #endregion


    #region UNITY METHODS

    private void Awake()
    {
        SetupInstance();
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);

        if (Instance != this)
        {
            return;
        }

        LoadCharacterData();
        LoadLockState();

        StartCoroutine(SetupCharacterDefaultPhotos());
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


    #region CHARACTER DATA METHODS

    private void LoadCharacterData()
    {
        if (characterDatas == null)
        {
            return;
        }

        for (int i = 0; i < characterDatas.Length; i++)
        {
            CharacterDataSO character = characterDatas[i];

            if (character == null)
            {
                continue;
            }

            character.GetSavedAffectionLevelValue();
            character.BuildSystemPrompt();
            character.SetListPhotoNumberOfElement();
        }
    }

    public CharacterDataSO GetCharacterDataSO(int index)
    {
        if (characterDatas == null ||
            index < 0 ||
            index >= characterDatas.Length)
        {
            Debug.LogWarning($"[CharacterDataManager] Invalid character index: {index}");

            return null;
        }

        return characterDatas[index];
    }

    #endregion


    #region CHARACTER PHOTO METHODS

    private IEnumerator SetupCharacterDefaultPhotos()
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

    #endregion


    #region CHARACTER LOCK METHODS

    private bool IsDefaultUnlocked(int index)
    {
        if (defaultUnlockedIndexes == null)
        {
            return false;
        }

        for (int i = 0; i < defaultUnlockedIndexes.Length; i++)
        {
            if (defaultUnlockedIndexes[i] == index)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsCharacterLocked(int index)
    {
        if (!IsValidCharacterIndex(index))
        {
            return true;
        }

        if (IsDefaultUnlocked(index))
        {
            return false;
        }

        if (saveLockData == null ||
            saveLockData.isLocked == null ||
            index >= saveLockData.isLocked.Length)
        {
            return true;
        }

        return saveLockData.isLocked[index];
    }

    public bool IsCharacterUnlocked(int index)
    {
        return IsValidCharacterIndex(index) && !IsCharacterLocked(index);
    }

    public void UnlockCharacter(int index)
    {
        if (!IsValidCharacterIndex(index))
        {
            Debug.LogWarning(
                $"[CharacterDataManager] Cannot unlock invalid index: {index}"
            );

            return;
        }

        EnsureLockStateInitialized();

        if (!saveLockData.isLocked[index])
        {
            return;
        }

        saveLockData.isLocked[index] = false;

        SaveLockState();

        Debug.Log(
            $"[CharacterDataManager] Character {index} unlocked."
        );
    }

    public int GetUnlockedCount()
    {
        if (characterDatas == null)
        {
            return 0;
        }

        int unlockedCount = 0;

        for (int i = 0; i < characterDatas.Length; i++)
        {
            if (IsCharacterUnlocked(i))
            {
                unlockedCount++;
            }
        }

        return unlockedCount;
    }

    public void ResetLockState()
    {
        int count = characterDatas != null
            ? characterDatas.Length
            : 0;

        saveLockData = new CharacterLockStateData
        {
            isLocked = new bool[count]
        };

        for (int i = 0; i < count; i++)
        {
            saveLockData.isLocked[i] = !IsDefaultUnlocked(i);
        }

        SaveLockState();

        Debug.Log(
            "[CharacterDataManager] Character lock state reset."
        );
    }

    private bool IsValidCharacterIndex(int index)
    {
        return characterDatas != null &&
               index >= 0 &&
               index < characterDatas.Length &&
               characterDatas[index] != null;
    }

    private void EnsureLockStateInitialized()
    {
        int count = characterDatas != null ? characterDatas.Length : 0;

        if (saveLockData != null &&
            saveLockData.isLocked != null &&
            saveLockData.isLocked.Length == count)
        {
            return;
        }

        bool[] previousStates = saveLockData != null ? saveLockData.isLocked : null;
        bool[] newStates = new bool[count];

        for (int i = 0; i < count; i++)
        {
            newStates[i] = previousStates != null && i < previousStates.Length ? previousStates[i] : !IsDefaultUnlocked(i);

            if (IsDefaultUnlocked(i))
            {
                newStates[i] = false;
            }
        }

        saveLockData = new CharacterLockStateData
        {
            isLocked = newStates
        };
    }

    #endregion


    #region SAVE AND LOAD METHODS

    private void LoadLockState()
    {
        if (!File.Exists(SavePath))
        {
            ResetLockState();
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);

            saveLockData = JsonUtility.FromJson<CharacterLockStateData>(json);

            EnsureLockStateInitialized();

            // Default characters must always remain unlocked.
            for (int i = 0; i < saveLockData.isLocked.Length; i++)
            {
                if (IsDefaultUnlocked(i))
                {
                    saveLockData.isLocked[i] = false;
                }
            }

            SaveLockState();
        }
        catch (Exception e)
        {
            Debug.LogError($"[CharacterDataManager] Load lock state failed: {e.Message}");

            ResetLockState();
        }
    }

    private void SaveLockState()
    {
        try
        {
            EnsureLockStateInitialized();

            string json = JsonUtility.ToJson(
                saveLockData,
                true
            );

            File.WriteAllText(SavePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CharacterDataManager] Save lock state failed: {e.Message}");
        }
    }

    #endregion
}
