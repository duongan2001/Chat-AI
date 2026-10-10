using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "AI Data/Character Data")]
public class CharacterDataSO : ScriptableObject
{
    #region VARIABLES

    [Header("--- BASIC INFO ---")]
    public int characterId;
    public string characterName;
    public int age;

    public GenderType gender;
    public CharacterType characterType;

    [Header("--- SHORT DESCRIPTION ---")]
    [TextArea(2, 5)]
    public string shortDescription;

    [TextArea(2, 5)]
    public string[] shortDescriptionMultiLanguages;

    [Header("--- APPEARANCE ---")]
    [TextArea(2, 5)]
    public string appearance;

    [Header("--- PERSONALITIES ---")]
    public List<PersonalityType> personalities;
    public string[] personalityMultiLanguages1;
    public string[] personalityMultiLanguages2;
    public string[] personalityMultiLanguages3;
    public string[] personalityMultiLanguages4;

    [Header("--- RELATIONSHIP GOAL ---")]
    [TextArea(3, 6)]
    public string relationshipGoal;
    public string[] relationshipGoalMultiLanguages;

    [Header("--- AFFECTION LEVEL ---")]
    [Range(0f, 100f)]
    public int affectionLevel;

    [Header("--- BACKROUND & SPEAKING STYLE ---")]
    [TextArea(3, 6)]
    public string background;

    [TextArea(2, 5)]
    public string speakingStyle;

    [Header("--- PHOTO ---")]
    public int numberOfPhoto;
    public string[] photoUrls;
    public Sprite[] photoSprites;

    [Header("--- MESSAGE COUNT ---")]
    public string messageCount;

    [TextArea(5, 12)]
    [Header("--- AI CONFIGURATION ---")]
    public string systemPrompt;

    private string AffectionSaveKey => $"{PlayerPrefKey.AffectionLevel}_{characterName}";

    private string PhotoDirectory => Path.Combine(Application.persistentDataPath, "CharacterPhotos", characterId.ToString());

    #endregion


    #region AFFECTION LEVEL METHODS

    public void GetSavedAffectionLevelValue()
    {
        if (PlayerPrefs.HasKey(AffectionSaveKey))
        {
            affectionLevel = Mathf.Clamp(PlayerPrefs.GetInt(AffectionSaveKey), 0, 100);
        }
    }

    public void SetAffectionLevelValue(int value)
    {
        affectionLevel = Mathf.Clamp(value, 0, 100);

        PlayerPrefs.SetInt(AffectionSaveKey, affectionLevel);
        PlayerPrefs.Save();

        BuildSystemPrompt();
    }

    private string GetAffectionDescription(int level)
    {
        if (level <= 20)
        {
            return "Stranger: She is polite but reserved. " +
                   "She maintains emotional distance and avoids " +
                   "romantic or overly personal conversations.";
        }

        if (level <= 40)
        {
            return "Acquaintance: She becomes more comfortable " +
                   "and friendly. She shows curiosity about the " +
                   "user and enjoys casual conversations.";
        }

        if (level <= 60)
        {
            return "Close Friend: She trusts the user and " +
                   "communicates openly. She jokes, teases " +
                   "playfully, and shows genuine care.";
        }

        if (level <= 80)
        {
            return "Romantic Interest: She feels emotionally " +
                   "connected to the user. She enjoys flirting, " +
                   "expresses affection, and becomes more romantic.";
        }

        return "Deep Love: She deeply loves and trusts the user. " +
               "She is affectionate, emotionally intimate, " +
               "supportive, and openly expresses romantic feelings.";
    }

    #endregion


    #region SYSTEM PROMPT METHODS

    public void BuildSystemPrompt()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("[SYSTEM INSTRUCTION: CHARACTER ROLEPLAY]");

        sb.AppendLine(
            $"You must strictly stay in character as {characterName}. " +
            "Maintain your personality, emotions, background, " +
            "and relationship with the user throughout the conversation."
        );

        sb.AppendLine();
        sb.AppendLine("=== CHARACTER PROFILE ===");

        sb.AppendLine($"- Name: {characterName}");
        sb.AppendLine($"- Age: {age}");
        sb.AppendLine($"- Gender: {gender}");
        sb.AppendLine($"- Character Style: {characterType}");

        if (!string.IsNullOrWhiteSpace(appearance))
        {
            sb.AppendLine($"- Appearance: {appearance}");
        }

        string personalityText = personalities != null && personalities.Count > 0 ? string.Join(", ", personalities) : "None";

        sb.AppendLine($"- Personality Traits: {personalityText}");

        sb.AppendLine($"- Affection Level with User: {affectionLevel}/100 - ({GetAffectionDescription(affectionLevel)})");

        if (!string.IsNullOrWhiteSpace(background))
        {
            sb.AppendLine();
            sb.AppendLine("=== CHARACTER BACKGROUND ===");
            sb.AppendLine(background);
        }

        if (!string.IsNullOrWhiteSpace(speakingStyle))
        {
            sb.AppendLine();
            sb.AppendLine("=== SPEAKING STYLE ===");
            sb.AppendLine(speakingStyle);
        }

        sb.AppendLine();
        sb.AppendLine("=== BEHAVIOR INSTRUCTIONS ===");

        sb.AppendLine("- Stay consistent with the character profile.");
        sb.AppendLine("- Express emotions naturally based on the current affection level.");
        sb.AppendLine("- Gradually develop emotional closeness as affection increases.");
        sb.AppendLine("- Use a natural, conversational speaking style.");
        sb.AppendLine("- Avoid overly formal or robotic responses.");
        sb.AppendLine("- Keep most replies short and suitable for mobile chat.");
        sb.AppendLine("- Respond in the same language as the user unless requested otherwise.");
        sb.AppendLine("- Do not invent shared past events or memories that were not established.");

        systemPrompt = sb.ToString();
    }

    #endregion


    #region PHOTO METHODS

    public void SetListPhotoNumberOfElement()
    {
        int count = Mathf.Max(0, numberOfPhoto);

        if (photoSprites == null)
        {
            photoSprites = new Sprite[count];
            return;
        }

        if (photoSprites.Length != count)
        {
            Array.Resize(ref photoSprites, count);
        }
    }

    public IEnumerator DownloadPhoto(int id)
    {
        if (!IsValidPhotoId(id))
        {
            Debug.LogWarning($"[CharacterDataSO] Invalid photo ID: {id}");
            yield break;
        }

        SetListPhotoNumberOfElement();

        if (IsPhotoDownloaded(id))
        {
            LoadSavedPhotoToSprite(id);
            yield break;
        }

        if (photoUrls == null ||
            id >= photoUrls.Length ||
            string.IsNullOrWhiteSpace(photoUrls[id]))
        {
            Debug.LogWarning($"[CharacterDataSO] Photo URL not found: {id}");
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequest.Get(photoUrls[id]))
        {
            request.timeout = 30;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[CharacterDataSO] Download failed: {request.error}");
                yield break;
            }

            byte[] imageBytes = request.downloadHandler.data;

            if (imageBytes == null || imageBytes.Length == 0)
            {
                Debug.LogError("[CharacterDataSO] Downloaded image is empty.");
                yield break;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            if (!ImageConversion.LoadImage(texture, imageBytes))
            {
                Destroy(texture);

                Debug.LogError("[CharacterDataSO] Invalid image data.");
                yield break;
            }

            try
            {
                Directory.CreateDirectory(PhotoDirectory);

                File.WriteAllBytes(GetPhotoPath(id), imageBytes);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CharacterDataSO] Failed to save photo: {e.Message}");

                Destroy(texture);
                yield break;
            }

            photoSprites[id] = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );
        }
    }

    public void LoadSavedPhotoToSprite(int id)
    {
        if (!IsValidPhotoId(id))
        {
            return;
        }

        string path = GetPhotoPath(id);

        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            byte[] imageBytes = File.ReadAllBytes(path);

            Texture2D texture = new Texture2D(
                2,
                2,
                TextureFormat.RGBA32,
                false
            );

            if (!ImageConversion.LoadImage(texture, imageBytes))
            {
                Destroy(texture);
                return;
            }

            SetListPhotoNumberOfElement();

            photoSprites[id] = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[CharacterDataSO] Failed to load photo: {e.Message}"
            );
        }
    }

    public bool IsPhotoDownloaded(int id)
    {
        if (!IsValidPhotoId(id))
        {
            return false;
        }

        return File.Exists(GetPhotoPath(id));
    }

    #endregion


    #region PHOTO HELPER METHODS

    private bool IsValidPhotoId(int id)
    {
        return id >= 0 && id < numberOfPhoto;
    }

    private string GetPhotoPath(int id)
    {
        return Path.Combine(
            PhotoDirectory,
            $"Photo_{id}.png"
        );
    }

    public void DeleteCachedPhoto(int id)
    {
        if (id < 0 || id >= numberOfPhoto)
        {
            return;
        }

        string path = GetPhotoPath(id);

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            if (photoSprites != null &&
                id < photoSprites.Length)
            {
                photoSprites[id] = null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[CharacterDataSO] Delete cached photo failed: {e.Message}");
        }
    }



    #endregion


    #region LOCALIZATION METHODS

    public string GetRelationshipGoalString()
    {
        return GetLocalizedValue(relationshipGoalMultiLanguages, relationshipGoal);
    }

    public string GetPersonalityString(int index)
    {
        string[] localizedValues = null;

        switch (index)
        {
            case 0:
                localizedValues = personalityMultiLanguages1;
                break;

            case 1:
                localizedValues = personalityMultiLanguages2;
                break;

            case 2:
                localizedValues = personalityMultiLanguages3;
                break;

            case 3:
                localizedValues = personalityMultiLanguages4;
                break;
        }

        string fallback = string.Empty;

        if (personalities != null &&
            index >= 0 &&
            index < personalities.Count)
        {
            fallback = personalities[index].ToString();
        }

        return GetLocalizedValue(localizedValues, fallback);
    }

    public string GetShortDescription()
    {
        return GetLocalizedValue(
            shortDescriptionMultiLanguages,
            shortDescription
        );
    }

    private string GetLocalizedValue(string[] translations, string fallback)
    {
        int languageIndex = GetCurrentLanguageIndex();

        if (translations != null &&
            languageIndex >= 0 &&
            languageIndex < translations.Length &&
            !string.IsNullOrWhiteSpace(translations[languageIndex]))
        {
            return translations[languageIndex];
        }

        // Fallback to English.
        if (translations != null &&
            translations.Length > 0 &&
            !string.IsNullOrWhiteSpace(translations[0]))
        {
            return translations[0];
        }

        return fallback ?? string.Empty;
    }

    private int GetCurrentLanguageIndex()
    {
        if (MultiLanguageManager.Instance != null)
        {
            return MultiLanguageManager.Instance.GetCurrentLanguageId();
        }

        return (int)Language.En;
    }

    #endregion
}
