// DeepSeekChatManager.cs
// ================================================================
// ⚠️ BẢN TEST NHANH — gọi thẳng DeepSeek API từ Unity, KHÔNG qua backend.
// API key nằm ngay trong Inspector/code, sẽ lộ nếu build ra APK/IPA thật.
// CHỈ dùng để test trong Unity Editor. Trước khi phát hành, chuyển
// sang gọi qua backend (server.js) để giấu key an toàn.
//
// DeepSeek dùng format API tương thích OpenAI (khác cấu trúc Gemini):
// - Endpoint: https://api.deepseek.com/chat/completions
// - Auth: header "Authorization: Bearer <key>" (khác Gemini để key trên URL)
// - messages là 1 mảng phẳng gồm role "system"/"user"/"assistant"
//   (Gemini tách riêng systemInstruction, DeepSeek gộp chung vào messages)
//
// Yêu cầu: cài package "com.unity.nuget.newtonsoft-json" qua Package Manager
// (Window → Package Manager → + → Add package by name)
//
// Interface (SendMessage, ParsedChatReply, LoadHistory...) giữ giống hệt
// GeminiChatManager.cs để có thể thay thế qua lại dễ dàng trong ChatUIController.
// ================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

// Lưu ý: nếu project đã có ChatHistoryItem/ParsedChatReply từ GeminiChatManager.cs,
// KHÔNG khai báo lại ở đây để tránh lỗi trùng tên class (CS0101).
// Nếu bạn dùng DeepSeekChatManager thay thế hoàn toàn Gemini, hãy xoá 2 class này
// khỏi GeminiChatManager.cs, hoặc ngược lại — chỉ giữ 1 bản duy nhất trong project.

public class DeepSeekChatManager : MonoBehaviour
{
    //==================== SINGLETON ====================
    public static DeepSeekChatManager Instance;


    //==================== SETUP ====================
    [Header("⚠️ TEST ONLY - API Key")]
    [Tooltip("Dán API key lấy từ platform.deepseek.com. CHỈ dùng để test, không build ra app thật với key ở đây.")]
    public string deepSeekApiKey = "PASTE_YOUR_DEEPSEEK_API_KEY_HERE";

    [Tooltip("Model dùng để chat — deepseek-chat trỏ tới V4 Flash (non-thinking mode)")]
    public string modelName = "deepseek-chat";

    private const string DeepSeekEndpoint = "https://api.deepseek.com/chat/completions";

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
        string jsonBody = BuildRequestJson(userMessage, systemPrompt);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(DeepSeekEndpoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            // DeepSeek xác thực qua header Authorization (khác Gemini để key trên URL)
            request.SetRequestHeader("Authorization", "Bearer " + deepSeekApiKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("DeepSeek request failed: " + request.error + "\n" + request.downloadHandler.text);
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
                Debug.LogError("Failed to parse DeepSeek response: " + e.Message + "\n" + request.downloadHandler.text);
                onError?.Invoke("Failed to parse response");
                yield break;
            }

            if (string.IsNullOrEmpty(replyText))
            {
                onError?.Invoke("Empty reply from DeepSeek. Raw response: " + request.downloadHandler.text);
                yield break;
            }

            // Lưu ý: DeepSeek dùng role "assistant" thay vì "model" (khác Gemini).
            // Ta vẫn lưu nội bộ là "model" cho đồng bộ với ChatHistoryItem đã định nghĩa,
            // rồi map lại thành "assistant" khi build request (xem BuildRequestJson).
            chatHistory.Add(new ChatHistoryItem { role = "user", text = userMessage });
            chatHistory.Add(new ChatHistoryItem { role = "model", text = replyText });

            onSuccess?.Invoke(ParseReply(replyText));
        }
    }

    /// <summary>
    /// Build JSON request theo format DeepSeek (tương thích OpenAI Chat Completions).
    /// Khác biệt lớn nhất so với Gemini: system prompt nằm chung trong mảng "messages"
    /// với role "system", KHÔNG tách riêng thành field "systemInstruction".
    /// </summary>
    private string BuildRequestJson(string userMessage, string systemPrompt)
    {
        var messagesArray = new JArray();

        // System prompt luôn là message đầu tiên
        messagesArray.Add(new JObject
        {
            ["role"] = "system",
            ["content"] = systemPrompt
        });

        foreach (var item in chatHistory)
        {
            // DeepSeek dùng "assistant" thay vì "model"
            string role = item.role == "user" ? "user" : "assistant";
            messagesArray.Add(new JObject
            {
                ["role"] = role,
                ["content"] = item.text
            });
        }

        // Tin nhắn mới nhất của user
        messagesArray.Add(new JObject
        {
            ["role"] = "user",
            ["content"] = userMessage
        });

        var root = new JObject
        {
            ["model"] = modelName,
            ["messages"] = messagesArray,
            ["temperature"] = 1.3, // DeepSeek khuyến nghị temperature cao hơn cho văn phong tự nhiên/sáng tạo
            ["max_tokens"] = 400,
            ["stream"] = false
        };

        return root.ToString(Newtonsoft.Json.Formatting.None);
    }

    /// <summary>
    /// Parse response DeepSeek (format giống OpenAI Chat Completions):
    /// choices[0].message.content
    /// </summary>
    private string ExtractReplyText(string rawJson)
    {
        JObject response = JObject.Parse(rawJson);

        JToken textToken = response["choices"]?[0]?["message"]?["content"];

        // Log token usage để theo dõi chi phí khi test (DeepSeek cũng trả usageMetadata dạng OpenAI)
        JToken usage = response["usage"];
        if (usage != null)
        {
            int promptTokens = usage["prompt_tokens"]?.ToObject<int>() ?? 0;
            int outputTokens = usage["completion_tokens"]?.ToObject<int>() ?? 0;
            int totalTokens = usage["total_tokens"]?.ToObject<int>() ?? 0;

            // DeepSeek trả thêm chi tiết cache hit/miss trong prompt_tokens_details (nếu có)
            int cachedTokens = usage["prompt_tokens_details"]?["cached_tokens"]?.ToObject<int>() ?? 0;

            Debug.Log($"[DeepSeek usage] prompt: {promptTokens} tokens (cached: {cachedTokens}), " +
                      $"output: {outputTokens} tokens, total: {totalTokens} tokens");
        }

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
