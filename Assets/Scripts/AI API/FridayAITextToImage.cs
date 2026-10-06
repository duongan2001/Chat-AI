using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Script tạo ảnh từ text qua FRIDAYAI (fridayaix.com) — API Gateway Việt Nam tương thích chuẩn OpenAI.
/// Chỉ khác OpenAI gốc ở Base URL + API key; format request/response giữ nguyên chuẩn OpenAI.
///
/// ⚠️ CẢNH BÁO: Tài liệu công khai của FridayAI (docs.fridayaix.com) hiện chỉ liệt kê các endpoint
/// chat/coding (/v1/chat/completions, /v1/responses) cho Claude/GPT/Gemini — KHÔNG thấy đề cập
/// endpoint sinh ảnh (/v1/images/generations). Trước khi dùng script này cho sản phẩm thật, hãy:
///   1. Test thử 1 request để xác nhận FridayAI có thực sự forward được tới GPT Image hay không.
///   2. Nếu lỗi 404/400, liên hệ FridayAI (fridayaix@gmail.com) hỏi xem họ có support model sinh ảnh không.
///
/// Gắn script này vào 1 GameObject trong Scene, kéo RawImage vào để hiển thị kết quả.
/// </summary>
public class FridayAITextToImage : MonoBehaviour
{
    [Header("API Config")]
    [Tooltip("API Key lấy từ oneai.fridayaix.com/keys (dạng bắt đầu bằng 'sk_' hoặc 'key_')")]
    [SerializeField] private string apiKey = "YOUR_FRIDAYAI_API_KEY";

    [Tooltip("Base URL của FridayAI — KHÁC DUY NHẤT so với script OpenAI gốc")]
    [SerializeField] private string baseUrl = "https://oneai.fridayaix.com";

    [Tooltip("Model sinh ảnh. Đây là tên model OpenAI gốc — FridayAI có thể yêu cầu tên khác, kiểm tra qua GET /v1/models nếu lỗi 'model not found'.")]
    [SerializeField] private string model = "gpt-image-2";

    [Tooltip("Kích thước ảnh: 1024x1024 (rẻ nhất), 1536x1024 (ngang), 1024x1536 (dọc)")]
    [SerializeField] private string size = "1024x1024";

    [Tooltip("Chất lượng: low (rẻ nhất), medium, high — giá tương đương hoặc rẻ hơn gpt-image-1.5")]
    [SerializeField] private string quality = "low";

    [Header("UI (tùy chọn)")]
    [SerializeField] private RawImage displayImage;

    [Header("Retry khi gặp lỗi 429 (Too Many Requests)")]
    [SerializeField] private int maxRetries = 3;
    [SerializeField] private float retryDelaySeconds = 5f;

    [Header("Kết quả")]
    public Texture2D resultTexture;

    // Endpoint ghép động từ baseUrl thay vì hard-code như bản OpenAI gốc,
    // vì FridayAI dùng domain riêng (oneai.fridayaix.com) thay vì api.openai.com
    private string GenerationsUrl => $"{baseUrl}/v1/images/generations";
    private string EditsUrl => $"{baseUrl}/v1/images/edits";
    private string ModelsUrl => $"{baseUrl}/v1/models";

    /// <summary>
    /// Gọi hàm này (ví dụ trong Start(), hoặc gán vào 1 nút "Test kết nối" trên UI)
    /// để kiểm tra xem đã kết nối được với FridayAI hay chưa, TRƯỚC KHI thử sinh ảnh thật.
    /// Gọi GET /v1/models — nếu thành công nghĩa là API key hợp lệ và server phản hồi được.
    /// Không tốn credit vì đây chỉ là liệt kê danh sách model, không phải request sinh ảnh.
    /// </summary>
    [ContextMenu("Test Connection")]
    public void CheckConnection()
    {
        StartCoroutine(CheckConnectionRoutine());
    }

    private IEnumerator CheckConnectionRoutine()
    {
        Debug.Log($"[FridayAI] Đang kiểm tra kết nối tới {baseUrl} ...");

        using (UnityWebRequest request = UnityWebRequest.Get(ModelsUrl))
        {
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                // Phân biệt rõ lỗi mạng (không tới được server) và lỗi xác thực/server (tới được nhưng bị từ chối)
                if (request.result == UnityWebRequest.Result.ConnectionError)
                {
                    Debug.LogError($"[FridayAI] ❌ KHÔNG kết nối được tới server ({baseUrl}). Kiểm tra lại mạng, Base URL, hoặc firewall. Chi tiết: {request.error}");
                }
                else if (request.responseCode == 401 || request.responseCode == 403)
                {
                    Debug.LogError($"[FridayAI] ❌ Kết nối được tới server, nhưng API key SAI hoặc hết hạn (mã lỗi {request.responseCode}). Kiểm tra lại apiKey trong Inspector.");
                }
                else
                {
                    Debug.LogError($"[FridayAI] ❌ Kết nối được nhưng server trả lỗi ({request.responseCode}): {request.downloadHandler.text}");
                }

                yield break;
            }

            Debug.Log("[FridayAI] ✅ Kết nối thành công! API key hợp lệ, server phản hồi bình thường.");

            // Log danh sách model để tiện kiểm tra xem có model sinh ảnh (gpt-image-*) hay không
            string responseText = request.downloadHandler.text;
            LogAvailableImageModels(responseText);
        }
    }

    /// <summary>
    /// Duyệt response của /v1/models, tìm và log các model có khả năng liên quan tới sinh ảnh.
    /// Giúp xác định nhanh FridayAI có thực sự hỗ trợ gpt-image-* hay không, không cần đoán.
    /// </summary>
    private void LogAvailableImageModels(string modelsJson)
    {
        try
        {
            JObject root = JObject.Parse(modelsJson);
            var data = root["data"] as JArray;

            if (data == null || data.Count == 0)
            {
                Debug.LogWarning("[FridayAI] Server trả về danh sách model rỗng — không thể xác nhận model sinh ảnh nào khả dụng.");
                return;
            }

            bool foundImageModel = false;
            var allModelIds = new System.Text.StringBuilder();

            foreach (var m in data)
            {
                string id = m["id"]?.ToString();
                if (string.IsNullOrEmpty(id)) continue;

                allModelIds.Append(id).Append(", ");

                if (id.Contains("image") || id.Contains("dall-e"))
                {
                    foundImageModel = true;
                    Debug.Log($"[FridayAI] 🎨 Tìm thấy model sinh ảnh khả dụng: {id}");
                }
            }

            if (!foundImageModel)
            {
                Debug.LogWarning($"[FridayAI] ⚠️ KHÔNG tìm thấy model sinh ảnh nào (gpt-image-*, dall-e-*) trong danh sách model được hỗ trợ. Danh sách model hiện có: {allModelIds}");
            }
        }
        catch (JsonException e)
        {
            Debug.LogWarning("[FridayAI] Không parse được danh sách model để kiểm tra (không ảnh hưởng kết nối chính): " + e.Message);
        }
    }

    /// <summary>
    /// Gọi hàm này với 1 chuỗi prompt để tạo ảnh mới hoàn toàn.
    /// Ví dụ: GenerateImage("một con mèo phi hành gia trên mặt trăng, phong cách pixar");
    /// </summary>
    public void GenerateImage(string prompt)
    {
        StartCoroutine(GenerateImageRoutine(prompt));
    }

    /// <summary>
    /// Gọi hàm này với prompt + 1 ảnh input để EDIT ảnh đó theo mô tả text.
    /// Dùng endpoint riêng /v1/images/edits, gửi dạng multipart/form-data.
    /// Ví dụ: EditImage("đổi nền thành bãi biển hoàng hôn", myTexture);
    /// </summary>
    public void EditImage(string prompt, Texture2D inputImage)
    {
        StartCoroutine(EditImageRoutine(prompt, inputImage));
    }

    private IEnumerator EditImageRoutine(string prompt, Texture2D inputImage)
    {
        Debug.Log($"[FridayAI] Đang gửi yêu cầu EDIT ảnh tới {EditsUrl} (model: {model})...");

        // /v1/images/edits yêu cầu multipart/form-data, không phải JSON như /generations.
        // OpenAI yêu cầu ảnh input dạng PNG.
        byte[] pngBytes = inputImage.EncodeToPNG();

        WWWForm form = new WWWForm();
        form.AddField("model", model);
        form.AddField("prompt", prompt);
        form.AddField("size", size);
        form.AddBinaryData("image", pngBytes, "input.png", "image/png");

        int attempt = 0;

        while (true)
        {
            attempt++;

            using (UnityWebRequest request = UnityWebRequest.Post(EditsUrl, form))
            {
                request.SetRequestHeader("Authorization", "Bearer " + apiKey);

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string errorBody = request.downloadHandler.text;

                    if (request.responseCode == 429 && attempt <= maxRetries)
                    {
                        Debug.LogWarning($"[FridayAI] 429 Too Many Requests (lần {attempt}/{maxRetries}). Thử lại sau {retryDelaySeconds:F1}s...");
                        yield return new WaitForSeconds(retryDelaySeconds);
                        continue;
                    }

                    Debug.LogError($"[FridayAI] Lỗi request edit ({request.responseCode}): {request.error}\n{errorBody}");
                    yield break;
                }

                ProcessImageResponse(request.downloadHandler.text);
                yield break;
            }
        }
    }

    private IEnumerator GenerateImageRoutine(string prompt)
    {
        Debug.Log($"[FridayAI] Đang gửi yêu cầu TẠO ảnh tới {GenerationsUrl} (model: {model})...");

        var requestObj = new JObject
        {
            ["model"] = model,
            ["prompt"] = prompt,
            ["size"] = size,
            ["quality"] = quality,
            ["n"] = 1
        };

        string jsonBody = requestObj.ToString(Formatting.None);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        int attempt = 0;

        while (true)
        {
            attempt++;

            using (UnityWebRequest request = new UnityWebRequest(GenerationsUrl, "POST"))
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
                        Debug.LogWarning($"[FridayAI] 429 Too Many Requests (lần {attempt}/{maxRetries}). Thử lại sau {retryDelaySeconds:F1}s...");
                        yield return new WaitForSeconds(retryDelaySeconds);
                        continue;
                    }

                    Debug.LogError($"[FridayAI] Lỗi request ({request.responseCode}): {request.error}\n{errorBody}");
                    yield break;
                }

                ProcessImageResponse(request.downloadHandler.text);
                yield break;
            }
        }
    }

    /// <summary>
    /// Xử lý response chung cho cả generate và edit: parse base64, load vào Texture2D, gán UI.
    /// </summary>
    private void ProcessImageResponse(string responseText)
    {
        byte[] imageBytes = ExtractImageBytesFromResponse(responseText);

        if (imageBytes == null)
        {
            Debug.LogError("[FridayAI] Không tìm thấy ảnh trong response:\n" + responseText);
            return;
        }

        Texture2D tex = new Texture2D(2, 2);
        if (tex.LoadImage(imageBytes))
        {
            resultTexture = tex;

            if (displayImage != null)
            {
                displayImage.texture = tex;
            }

            Debug.Log("[FridayAI] Tạo/sửa ảnh thành công!");
        }
        else
        {
            Debug.LogError("[FridayAI] Không thể decode dữ liệu ảnh trả về.");
        }
    }

    /// <summary>
    /// Response OpenAI dạng: { "data": [ { "b64_json": "..." } ] }
    /// </summary>
    private byte[] ExtractImageBytesFromResponse(string json)
    {
        try
        {
            JObject root = JObject.Parse(json);
            var data = root["data"] as JArray;

            if (data == null || data.Count == 0)
            {
                Debug.LogWarning("[FridayAI] Response không có field 'data'.");
                return null;
            }

            string base64 = data[0]["b64_json"]?.ToString();

            if (string.IsNullOrEmpty(base64))
            {
                Debug.LogWarning("[FridayAI] Không có b64_json trong response (có thể response_format đang là 'url' thay vì base64).");
                return null;
            }

            return Convert.FromBase64String(base64);
        }
        catch (JsonException e)
        {
            Debug.LogError("[FridayAI] Lỗi parse JSON: " + e.Message);
            return null;
        }
        catch (FormatException e)
        {
            Debug.LogError("[FridayAI] Base64 không hợp lệ: " + e.Message);
            return null;
        }
    }
}