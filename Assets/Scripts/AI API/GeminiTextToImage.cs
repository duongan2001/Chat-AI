using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Script tạo ảnh từ text bằng Gemini API (model gemini-2.5-flash-image, còn gọi là "Nano Banana").
/// Gắn script này vào 1 GameObject trong Scene, kéo RawImage hoặc Image vào để hiển thị kết quả.
/// </summary>
public class GeminiTextToImage : MonoBehaviour
{
    [Header("API Config")]
    [Tooltip("API Key lấy từ Google AI Studio (aistudio.google.com)")]
    [SerializeField] private string apiKey = "YOUR_GEMINI_API_KEY";

    [Tooltip("Model hỗ trợ sinh ảnh")]
    [SerializeField] private string model = "gemini-2.5-flash-image";

    [Header("UI (tùy chọn)")]
    [SerializeField] private RawImage displayImage; // kéo RawImage vào đây để xem kết quả

    [Header("Kết quả")]
    public Texture2D resultTexture;

    private const string BASE_URL = "https://generativelanguage.googleapis.com/v1beta/models/";

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
        string url = $"{BASE_URL}{model}:generateContent?key={apiKey}";

        // Body request theo format Gemini generateContent, build bằng Newtonsoft.Json
        var requestObj = new JObject
        {
            ["contents"] = new JArray
            {
                new JObject
                {
                    ["parts"] = new JArray
                    {
                        new JObject { ["text"] = prompt }
                    }
                }
            },
            ["generationConfig"] = new JObject
            {
                ["responseModalities"] = new JArray { "IMAGE" }
            }
        };

        string jsonBody = requestObj.ToString(Formatting.None);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Gemini] Lỗi request: {request.error}\n{request.downloadHandler.text}");
                yield break;
            }

            string responseText = request.downloadHandler.text;
            byte[] imageBytes = ExtractImageBytesFromResponse(responseText);

            if (imageBytes == null)
            {
                Debug.LogError("[Gemini] Không tìm thấy ảnh trong response:\n" + responseText);
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

                Debug.Log("[Gemini] Tạo ảnh thành công!");
            }
            else
            {
                Debug.LogError("[Gemini] Không thể decode dữ liệu ảnh trả về.");
            }
        }
    }

    /// <summary>
    /// Tách phần inlineData.data (base64) trong JSON response của Gemini bằng Newtonsoft.Json.
    /// Response có dạng: candidates[0].content.parts[] -> mỗi part có thể là text hoặc inlineData.
    /// Hàm này duyệt qua các part để tìm part đầu tiên chứa ảnh.
    /// </summary>
    private byte[] ExtractImageBytesFromResponse(string json)
    {
        try
        {
            JObject root = JObject.Parse(json);

            var parts = root["candidates"]?[0]?["content"]?["parts"] as JArray;
            if (parts == null)
            {
                Debug.LogWarning("[Gemini] Response không có parts. Có thể bị chặn bởi safety filter, kiểm tra 'promptFeedback' trong response.");
                return null;
            }

            foreach (var part in parts)
            {
                string base64 = part["inlineData"]?["data"]?.ToString();
                if (!string.IsNullOrEmpty(base64))
                {
                    return Convert.FromBase64String(base64);
                }
            }

            Debug.LogWarning("[Gemini] Không có part nào chứa inlineData (ảnh).");
            return null;
        }
        catch (JsonException e)
        {
            Debug.LogError("[Gemini] Lỗi parse JSON: " + e.Message);
            return null;
        }
        catch (FormatException e)
        {
            Debug.LogError("[Gemini] Base64 không hợp lệ: " + e.Message);
            return null;
        }
    }
}