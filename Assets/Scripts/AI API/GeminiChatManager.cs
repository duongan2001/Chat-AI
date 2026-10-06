// GeminiChatManager.cs
// ================================================================
// ⚠️ BẢN TEST NHANH — gọi thẳng Gemini API từ Unity, KHÔNG qua backend.
// API key nằm ngay trong Inspector/code, sẽ lộ nếu build ra APK/IPA thật.
// CHỈ dùng để test trong Unity Editor. Trước khi phát hành, chuyển
// sang gọi qua backend (server.js) để giấu key an toàn.
//
// Yêu cầu: cài package "com.unity.nuget.newtonsoft-json" qua Package Manager
// (Window → Package Manager → + → Add package by name)
// ================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

/*
[Serializable]
public class ChatHistoryItem
{
    public string role; // "user" hoặc "model"
    public string text;
}

public class ParsedChatReply
{
    public string displayText;
    public bool hasImage;
    public string imagePromptDescription;
}
*/


public class GeminiChatManager : MonoBehaviour
{
    //==================== SINGLETON ====================
    public static GeminiChatManager Instance;


    //==================== SETUP ====================
    [Header("⚠️ TEST ONLY - API Key")]
    [Tooltip("Dán API key lấy từ aistudio.google.com/apikey. CHỈ dùng để test, không build ra app thật với key ở đây.")]
    public string geminiApiKey = "PASTE_YOUR_GEMINI_API_KEY_HERE";

    [Tooltip("Model dùng để chat")]
    public string modelName = "gemini-1.5-flash";

    private List<ChatHistoryItem> chatHistory = new List<ChatHistoryItem>();

    private static readonly Regex ImageTagRegex = new Regex(
        @"\[SEND_IMAGE:\s*(.+?)\]", RegexOptions.IgnoreCase | RegexOptions.Singleline);


    //==================== UNITY METHODS ====================
    private void Start()
    {
        Instance = this;
    }


    //==================== METHODS ====================
    public void SendMessage(string userId, string userMessage, int intimacyLevel,
        Action<ParsedChatReply> onSuccess, Action<string> onError)
    {
        string systemPrompt = CharacterPersona.BuildSystemPrompt(intimacyLevel);
        StartCoroutine(SendMessageCoroutine(userMessage, systemPrompt, onSuccess, onError));
    }

    private IEnumerator SendMessageCoroutine(string userMessage, string systemPrompt,
        Action<ParsedChatReply> onSuccess, Action<string> onError)
    {
        string url =
            $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={geminiApiKey}";

        string jsonBody = BuildRequestJson(userMessage, systemPrompt);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Gemini request failed: " + request.error + "\n" + request.downloadHandler.text);
                onError?.Invoke(request.error);
                yield break;
            }

            string replyText;
            try
            {
                replyText = ExtractReplyText(request.downloadHandler.text);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to parse Gemini response: " + e.Message + "\n" + request.downloadHandler.text);
                onError?.Invoke("Failed to parse response");
                yield break;
            }

            if (string.IsNullOrEmpty(replyText))
            {
                onError?.Invoke("Empty reply from Gemini. Raw response: " + request.downloadHandler.text);
                yield break;
            }

            chatHistory.Add(new ChatHistoryItem { role = "user", text = userMessage });
            chatHistory.Add(new ChatHistoryItem { role = "model", text = replyText });

            onSuccess?.Invoke(ParseReply(replyText));
        }
    }

    /// <summary>
    /// Build JSON request body theo format Gemini yêu cầu, dùng JObject/JArray của Newtonsoft
    /// thay vì ghép chuỗi thủ công — an toàn hơn với ký tự đặc biệt, dễ đọc/debug hơn.
    /// </summary>
    private string BuildRequestJson(string userMessage, string systemPrompt)
    {
        var contentsArray = new JArray();

        foreach (var item in chatHistory)
        {
            string role = item.role == "user" ? "user" : "model";
            contentsArray.Add(new JObject
            {
                ["role"] = role,
                ["parts"] = new JArray { new JObject { ["text"] = item.text } }
            });
        }

        // Thêm tin nhắn mới nhất của user
        contentsArray.Add(new JObject
        {
            ["role"] = "user",
            ["parts"] = new JArray { new JObject { ["text"] = userMessage } }
        });

        var root = new JObject
        {
            ["contents"] = contentsArray,
            ["systemInstruction"] = new JObject
            {
                ["parts"] = new JArray { new JObject { ["text"] = systemPrompt } }
            },
            ["generationConfig"] = new JObject
            {
                ["temperature"] = 0.9,
                ["maxOutputTokens"] = 400
            }
        };

        return root.ToString(Newtonsoft.Json.Formatting.None);
    }

    /// <summary>
    /// Parse JSON response từ Gemini bằng JObject.Parse — thay cho regex "tự chế" trước đây.
    /// An toàn hơn với text chứa dấu ngoặc kép, escape characters, hoặc cấu trúc JSON phức tạp.
    /// </summary>
    private string ExtractReplyText(string rawJson)
    {
        JObject response = JObject.Parse(rawJson);

        // Đường dẫn chuẩn trong response Gemini:
        // candidates[0].content.parts[0].text
        JToken textToken = response["candidates"]?[0]?["content"]?["parts"]?[0]?["text"];

        return textToken?.ToString();
    }

    private ParsedChatReply ParseReply(string rawReply)
    {
        var result = new ParsedChatReply { hasImage = false };

        Match match = ImageTagRegex.Match(rawReply);
        if (match.Success)
        {
            result.hasImage = true;
            result.imagePromptDescription = match.Groups[1].Value.Trim();
            result.displayText = ImageTagRegex.Replace(rawReply, "").Trim();
        }
        else
        {
            result.displayText = rawReply;
        }

        return result;
    }

    public void LoadHistory(List<ChatHistoryItem> savedHistory)
    {
        chatHistory = savedHistory ?? new List<ChatHistoryItem>();
    }

    public void ClearHistory()
    {
        chatHistory.Clear();
    }
}