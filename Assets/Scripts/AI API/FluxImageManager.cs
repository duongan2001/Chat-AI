// FluxImageManager.cs
// ================================================================
// ⚠️ BẢN TEST NHANH — gọi thẳng Fal.ai API từ Unity, KHÔNG qua backend.
// API key nằm ngay trong Inspector/code, sẽ lộ nếu build ra APK/IPA thật.
// CHỈ dùng để test trong Unity Editor. Trước khi phát hành, chuyển
// sang gọi qua backend (server.js) để giấu key an toàn.
// ================================================================

using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public class FluxImageManager : MonoBehaviour
{
    public static FluxImageManager Instance;

    [Header("⚠️ TEST ONLY - API Key")]
    [Tooltip("Dán API key lấy từ fal.ai dashboard. CHỈ dùng để test, không build ra app thật với key ở đây.")]
    public string falApiKey = "PASTE_YOUR_FAL_API_KEY_HERE";

    private const string FalEndpoint = "https://fal.run/fal-ai/flux/schnell";

    private void Awake()
    {
        Instance = this;
    }

    public void GenerateImage(string prompt, Action<Texture2D> onSuccess, Action<string> onError)
    {
        StartCoroutine(GenerateImageCoroutine(prompt, onSuccess, onError));
    }

    private IEnumerator GenerateImageCoroutine(string prompt, Action<Texture2D> onSuccess, Action<string> onError)
    {
        string jsonBody =
            "{" +
            $"\"prompt\":{EscapeJson(prompt)}," +
            "\"image_size\":\"portrait_4_3\"," +
            "\"num_inference_steps\":4," +
            "\"num_images\":1" +
            "}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        string imageUrl = null;

        using (UnityWebRequest request = new UnityWebRequest(FalEndpoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Key " + falApiKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Fal.ai request failed: " + request.error + "\n" + request.downloadHandler.text);
                onError?.Invoke(request.error);
                yield break;
            }

            imageUrl = ExtractImageUrl(request.downloadHandler.text);

            if (string.IsNullOrEmpty(imageUrl))
            {
                onError?.Invoke("No image URL in response: " + request.downloadHandler.text);
                yield break;
            }
        }

        using (UnityWebRequest textureRequest = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return textureRequest.SendWebRequest();

            if (textureRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Texture download failed: " + textureRequest.error);
                onError?.Invoke(textureRequest.error);
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(textureRequest);
            onSuccess?.Invoke(texture);
        }
    }

    /// <summary>
    /// Trích URL ảnh đầu tiên từ JSON response bằng regex đơn giản
    /// (tránh cần thư viện JSON ngoài cho bản test nhanh này).
    /// </summary>
    private string ExtractImageUrl(string rawJson)
    {
        Match match = Regex.Match(rawJson, "\"url\":\\s*\"(.*?)\"");
        return match.Success ? match.Groups[1].Value : null;
    }

    private string EscapeJson(string text)
    {
        return "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
    }
}
