using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Script tạo ảnh từ text bằng OpenAI Image API (model GPT Image, họ "gpt-image-1"/"gpt-image-1.5"/"gpt-image-2").
/// Gắn script này vào 1 GameObject trong Scene, kéo RawImage vào để hiển thị kết quả.
/// Lưu ý: cần API key OpenAI đã bật Organization Verification để dùng GPT Image models.
/// </summary>
public class OpenAITextToImage : MonoBehaviour
{
    [Header("API Config")]
    [Tooltip("API Key lấy từ platform.openai.com/api-keys")]
    [SerializeField] private string apiKey = "YOUR_OPENAI_API_KEY";

    [Tooltip("Model sinh ảnh: gpt-image-1, gpt-image-1.5, gpt-image-2, hoặc dall-e-3")]
    [SerializeField] private string model = "gpt-image-1";

    [Tooltip("Kích thước ảnh: 1024x1024, 1536x1024 (ngang), 1024x1536 (dọc), hoặc auto")]
    [SerializeField] private string size = "1024x1024";

    [Tooltip("Chất lượng: auto, high, medium, low (chỉ áp dụng cho GPT Image models)")]
    //[SerializeField] private string quality = "auto";
    [SerializeField] ImageQuality imageQuality;

    [Header("UI (tùy chọn)")]
    [SerializeField] private RawImage displayImage;

    [Header("Retry khi gặp lỗi 429 (Too Many Requests)")]
    [SerializeField] private int maxRetries = 3;
    [SerializeField] private float retryDelaySeconds = 5f;

    [Header("Kết quả")]
    public Texture2D resultTexture;

    private const string URL = "https://api.openai.com/v1/images/generations";

    /// <summary>
    /// Gọi hàm này với 1 chuỗi prompt để tạo ảnh.
    /// Ví dụ: GenerateImage("một con mèo phi hành gia trên mặt trăng, phong cách pixar");
    /// </summary>
    public void GenerateImage(string prompt)
    {
        StartCoroutine(GenerateImageRoutine(prompt));
    }

    private IEnumerator GenerateImageRoutine(string prompt)
    {
        var requestObj = new JObject
        {
            ["model"] = model,
            ["prompt"] = prompt,
            ["size"] = size,
            ["quality"] = GetImageGenQuality(),
            ["n"] = 1
        };

        string jsonBody = requestObj.ToString(Formatting.None);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        int attempt = 0;

        while (true)
        {
            attempt++;

            using (UnityWebRequest request = new UnityWebRequest(URL, "POST"))
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
                        Debug.LogWarning($"[OpenAI] 429 Too Many Requests (lần {attempt}/{maxRetries}). Thử lại sau {retryDelaySeconds:F1}s...");
                        yield return new WaitForSeconds(retryDelaySeconds);
                        continue;
                    }

                    Debug.LogError($"[OpenAI] Lỗi request ({request.responseCode}): {request.error}\n{errorBody}");
                    yield break;
                }

                string responseText = request.downloadHandler.text;
                byte[] imageBytes = ExtractImageBytesFromResponse(responseText);

                if (imageBytes == null)
                {
                    Debug.LogError("[OpenAI] Không tìm thấy ảnh trong response:\n" + responseText);
                    yield break;
                }

                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(imageBytes))
                {
                    resultTexture = tex;

                    if (displayImage != null)
                    {
                        displayImage.texture = tex;
                    }

                    Debug.Log("[OpenAI] Tạo ảnh thành công!");
                }
                else
                {
                    Debug.LogError("[OpenAI] Không thể decode dữ liệu ảnh trả về.");
                }

                yield break;
            }
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
                Debug.LogWarning("[OpenAI] Response không có field 'data'.");
                return null;
            }

            string base64 = data[0]["b64_json"]?.ToString();

            if (string.IsNullOrEmpty(base64))
            {
                Debug.LogWarning("[OpenAI] Không có b64_json trong response (có thể response_format đang là 'url' thay vì base64).");
                return null;
            }

            return Convert.FromBase64String(base64);
        }
        catch (JsonException e)
        {
            Debug.LogError("[OpenAI] Lỗi parse JSON: " + e.Message);
            return null;
        }
        catch (FormatException e)
        {
            Debug.LogError("[OpenAI] Base64 không hợp lệ: " + e.Message);
            return null;
        }
    }

    public enum ImageQuality { 
        low,
        medium,
        high,
        auto
    }

    string GetImageGenQuality()
    {
        switch(imageQuality)
        {
            default:
                return "low";

            case ImageQuality.low:
                return "low";

            case ImageQuality.medium:
                return "medium";

            case ImageQuality.high:
                return "high";

            case ImageQuality.auto:
                return "auto";
        }    
    }    

}