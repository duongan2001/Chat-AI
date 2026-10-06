using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Script trò chuyện (chat) qua FRIDAYAI (fridayaix.com) — dùng endpoint /v1/chat/completions,
/// đây là endpoint CHÍNH mà FridayAI có tài liệu công khai đầy đủ (khác với endpoint sinh ảnh).
/// Hỗ trợ Claude, GPT, Gemini... chỉ cần đổi tên model.
///
/// Gắn script này vào 1 GameObject, gọi SendMessage("câu hỏi") để nhận phản hồi text.
/// </summary>
public class FridayAIChat : MonoBehaviour
{
    public static FridayAIChat Instance;

    [Header("API Config")]
    [Tooltip("API Key lấy từ oneai.fridayaix.com/keys")]
    [SerializeField] private string apiKey = "YOUR_FRIDAYAI_API_KEY";

    [Tooltip("Base URL của FridayAI")]
    [SerializeField] private string baseUrl = "https://oneai.fridayaix.com";

    [Tooltip("Model chat. Đang dùng OpenAI. Các lựa chọn khác trên FridayAI: gpt-4o (đa phương thức, xử lý ảnh), gpt-4o-mini (rẻ nhất), claude-sonnet-4.6, gemini-2.5-pro")]
    [SerializeField] private string model = "gpt-4o-mini";

    [Tooltip("System prompt — vai trò/tính cách của AI trong suốt cuộc trò chuyện")]
    [TextArea(3, 4)]
    public string systemPrompt = "Bạn là một trợ lý AI hữu ích, trả lời ngắn gọn bằng tiếng Việt.";

    [Tooltip("Số token tối đa cho câu trả lời")]
    [SerializeField] private int maxTokens = 1024;

    [Tooltip("Độ sáng tạo: 0 = chính xác/lặp lại, 1-2 = sáng tạo/ngẫu nhiên hơn")]
    [Range(0f, 2f)]
    [SerializeField] private float temperature = 0.7f;

    [Header("Retry khi gặp lỗi 429")]
    [SerializeField] private int maxRetries = 3;
    [SerializeField] private float retryDelaySeconds = 5f;

    [Header("Kết quả")]
    [TextArea(3, 10)]
    public string lastReply;

    /// <summary>
    /// Bắn ra khi nhận được câu trả lời thành công. Đăng ký lắng nghe qua code, ví dụ:
    /// fridayChat.OnReplyReceived += (reply) => { chatText.text = reply; };
    /// </summary>
    public event Action<string> OnReplyReceived;

    /// <summary>
    /// Bắn ra khi có lỗi, kèm message lỗi để hiển thị cho người dùng nếu cần.
    /// </summary>
    public event Action<string> OnError;

    private string ChatUrl => $"{baseUrl}/v1/chat/completions";
    private string ModelsUrl => $"{baseUrl}/v1/models";


    private void Start()
    {
        Instance = this;
    }


    /// <summary>
    /// Gọi hàm này (ví dụ chuột phải component → "List Available Models" trong Editor,
    /// hoặc gán vào 1 nút debug) để xem CHÍNH XÁC danh sách model mà tài khoản/nhóm của bạn
    /// trên FridayAI được phép dùng — thay vì đoán mò rồi dính lỗi "not supported by any
    /// configured account in this group".
    /// </summary>
    [ContextMenu("List Available Models")]
    public void ListAvailableModels()
    {
        StartCoroutine(ListAvailableModelsRoutine());
    }

    private IEnumerator ListAvailableModelsRoutine()
    {
        Debug.Log($"[FridayAI Chat] Đang lấy danh sách model khả dụng từ {ModelsUrl} ...");

        using (UnityWebRequest request = UnityWebRequest.Get(ModelsUrl))
        {
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[FridayAI Chat] Không lấy được danh sách model ({request.responseCode}): {request.downloadHandler.text}");
                yield break;
            }

            try
            {
                JObject root = JObject.Parse(request.downloadHandler.text);
                var data = root["data"] as JArray;

                if (data == null || data.Count == 0)
                {
                    Debug.LogWarning("[FridayAI Chat] Danh sách model trả về rỗng.");
                    yield break;
                }

                var sb = new StringBuilder();
                sb.AppendLine($"[FridayAI Chat] ✅ Tài khoản bạn được phép dùng {data.Count} model:");
                foreach (var m in data)
                {
                    string id = m["id"]?.ToString();
                    if (!string.IsNullOrEmpty(id)) sb.AppendLine("  - " + id);
                }
                Debug.Log(sb.ToString());
            }
            catch (JsonException e)
            {
                Debug.LogError("[FridayAI Chat] Lỗi parse danh sách model: " + e.Message);
            }
        }
    }

    /// <summary>
    /// Gọi hàm này với 1 câu hỏi/tin nhắn để nhận phản hồi text từ AI.
    /// Đây là chat 1 lượt (không giữ lịch sử hội thoại) — mỗi lần gọi là 1 cuộc trò chuyện mới.
    /// </summary>
    public void SendMessage(string userMessage)
    {
        StartCoroutine(SendMessageRoutine(userMessage));
    }

    private IEnumerator SendMessageRoutine(string userMessage)
    {
        Debug.Log($"[FridayAI Chat] Đang gửi tin nhắn tới {ChatUrl} (model: {model})...");

        var requestObj = new JObject
        {
            ["model"] = model,
            ["messages"] = new JArray
            {
                new JObject { ["role"] = "system", ["content"] = systemPrompt },
                new JObject { ["role"] = "user", ["content"] = userMessage }
            },
            ["max_tokens"] = maxTokens,
            ["temperature"] = temperature
        };

        string jsonBody = requestObj.ToString(Formatting.None);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        int attempt = 0;

        while (true)
        {
            attempt++;

            using (UnityWebRequest request = new UnityWebRequest(ChatUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + apiKey);

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string errorBody = request.downloadHandler.text;

                    if (request.responseCode == 429 && attempt <= maxRetries)
                    {
                        Debug.LogWarning($"[FridayAI Chat] 429 Too Many Requests (lần {attempt}/{maxRetries}). Thử lại sau {retryDelaySeconds:F1}s...");
                        yield return new WaitForSeconds(retryDelaySeconds);
                        continue;
                    }

                    string errorMsg = ParseErrorMessage(errorBody, request.responseCode);
                    Debug.LogError($"[FridayAI Chat] Lỗi request ({request.responseCode}): {errorMsg}\n{errorBody}");
                    OnError?.Invoke(errorMsg);
                    yield break;
                }

                string reply = ExtractReplyFromResponse(request.downloadHandler.text);

                if (string.IsNullOrEmpty(reply))
                {
                    Debug.LogError("[FridayAI Chat] Không tìm thấy nội dung trả lời trong response:\n" + request.downloadHandler.text);
                    OnError?.Invoke("Không nhận được nội dung trả lời từ server.");
                    yield break;
                }

                lastReply = reply;
                Debug.Log($"[FridayAI Chat] ✅ Nhận được phản hồi: {reply}");
                OnReplyReceived?.Invoke(reply);
                yield break;
            }
        }
    }

    /// <summary>
    /// Response chuẩn OpenAI Chat Completions: { "choices": [ { "message": { "content": "..." } } ] }
    /// </summary>
    private string ExtractReplyFromResponse(string json)
    {
        try
        {
            JObject root = JObject.Parse(json);
            var choices = root["choices"] as JArray;

            if (choices == null || choices.Count == 0)
            {
                return null;
            }

            return choices[0]["message"]?["content"]?.ToString();
        }
        catch (JsonException e)
        {
            Debug.LogError("[FridayAI Chat] Lỗi parse JSON: " + e.Message);
            return null;
        }
    }

    /// <summary>
    /// Cố gắng lấy message lỗi cụ thể từ response, kèm phân biệt vài loại lỗi thường gặp.
    /// </summary>
    private string ParseErrorMessage(string errorJson, long statusCode)
    {
        try
        {
            JObject err = JObject.Parse(errorJson);
            string code = err["error"]?["code"]?.ToString();
            string message = err["error"]?["message"]?.ToString();

            if (code == "billing_hard_limit_reached")
            {
                return "Hệ thống backend đã chạm giới hạn billing (không phải do credit tài khoản bạn trên FridayAI). Vui lòng báo cho FridayAI để xử lý.";
            }

            string type = err["error"]?["type"]?.ToString();
            if (type == "model_not_found")
            {
                return $"Model \"{model}\" không được cấu hình cho tài khoản/nhóm của bạn. Gọi ListAvailableModels() (hoặc chuột phải component → \"List Available Models\") để xem danh sách model bạn thực sự được dùng.";
            }

            if (statusCode == 401 || statusCode == 403)
            {
                return "API key không hợp lệ hoặc hết hạn.";
            }

            return message ?? $"Lỗi không xác định (mã {statusCode}).";
        }
        catch
        {
            return $"Lỗi không xác định (mã {statusCode}).";
        }
    }
}