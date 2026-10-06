// ChatUIController.cs
// Controller chính cho màn hình chat. Nối GeminiChatManager, FluxImageManager,
// FirebaseChatStorage, và CharacterPersona lại với nhau.
//
// Setup trong Inspector: kéo thả reference cho chatManager, imageManager,
// InputField, Text hiển thị chat, RawImage hiển thị ảnh AI gửi.

using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ChatUIController : MonoBehaviour
{
    #region VARIABLES

    [Header("References")]
    //public GeminiChatManager chatManager;
    public FluxImageManager imageManager;

    public InputField userInputField;
    public Text chatDisplayText;    // Đơn giản hoá: thực tế nên dùng ScrollView + prefab tin nhắn
    public RawImage aiImageDisplay;
    public Text loadingIndicatorText; // Optional: hiện "Mika is typing..." khi đang chờ phản hồi

    [Header("References")]
    [SerializeField] int currentIntimacyLevel = 0;
    public string imgUrl;

    #endregion


    #region UNITY METHODS

    private void Start()
    {
        loadingIndicatorText.text = "Connecting...";

        // Bước 1: khởi tạo Firebase (auth ẩn danh + tạo profile nếu chưa có)
        /*
        FirebaseChatStorage.Instance.Initialize(
            onReady: () =>
            {
                // Bước 2: lấy intimacy level hiện tại
                FirebaseChatStorage.Instance.GetIntimacyLevel(
                    onSuccess: (level) =>
                    {
                        currentIntimacyLevel = level;
                        LoadChatHistory();
                    },
                    onError: (err) =>
                    {
                        Debug.LogError(err);
                        currentIntimacyLevel = 0;
                        LoadChatHistory();
                    }
                );
            },
            onError: (err) =>
            {
                Debug.LogError("Firebase init failed: " + err);
                loadingIndicatorText.text = "Connection failed. Please restart the app.";
            }
        );
        */
    }

    #endregion


    #region GEN CHAT METHODS

    /*
    private void LoadChatHistory()
    {
        FirebaseChatStorage.Instance.LoadRecentMessages(
            limit: 50,
            onSuccess: (messages) =>
            {
                // Hiển thị lại lịch sử chat lên UI
                chatDisplayText.text = "";
                var historyForGemini = new List<ChatHistoryItem>();

                foreach (var msg in messages)
                {
                    string label = msg.Role == "user" ? "You" : CharacterPersona.CharacterName;
                    AppendToChatDisplay($"{label}: {msg.Text}");

                    historyForGemini.Add(new ChatHistoryItem
                    {
                        role = msg.Role == "user" ? "user" : "model",
                        text = msg.Text
                    });
                }

                // Nạp lại lịch sử vào GeminiChatManager để giữ context hội thoại
                chatManager.LoadHistory(historyForGemini);

                loadingIndicatorText.text = "";
            },
            onError: (err) =>
            {
                Debug.LogError(err);
                loadingIndicatorText.text = "";
            }
        );
    }
    */

    public void OnSendButtonPressedGemini()
    {
        string userMessage = userInputField.text;
        if (string.IsNullOrWhiteSpace(userMessage)) return;

        AppendToChatDisplay("You: " + userMessage);
        userInputField.text = "";
        loadingIndicatorText.text = $"{CharacterPersona.CharacterName} is typing...";

        //string userId = FirebaseChatStorage.Instance.CurrentUserId;

        // Lưu tin nhắn của user lên Firestore ngay
        //FirebaseChatStorage.Instance.SaveMessage("user", userMessage);

        GeminiChatManager.Instance.SendMessage(
            //userId,
            "TestingUserID",
            userMessage,
            currentIntimacyLevel,
            onSuccess: (parsedReply) =>
            {
                loadingIndicatorText.text = "";
                AppendToChatDisplay($"{CharacterPersona.CharacterName}: {parsedReply.displayText}");

                // Lưu reply của AI lên Firestore
                //FirebaseChatStorage.Instance.SaveMessage("model", parsedReply.displayText);

                // Tăng điểm thân mật sau mỗi lượt trao đổi thành công
                currentIntimacyLevel = CharacterPersona.ClampIntimacy(
                    currentIntimacyLevel + CharacterPersona.CalculateIntimacyGain());
                //FirebaseChatStorage.Instance.UpdateIntimacyLevel(currentIntimacyLevel);

                // Nếu AI quyết định gửi ảnh (dựa theo tag [SEND_IMAGE: ...] đã được parse)
                if (parsedReply.hasImage)
                {
                    RequestAIImage(parsedReply.imagePromptDescription);
                }
            },
            onError: (err) =>
            {
                loadingIndicatorText.text = "";
                AppendToChatDisplay("[Error] Could not send message: " + err);
            }
        );
    }

    public void OnSendButtonPressedDeepSeek()
    {
        string userMessage = userInputField.text;
        if (string.IsNullOrWhiteSpace(userMessage)) return;

        AppendToChatDisplay("You: " + userMessage);
        userInputField.text = "";
        loadingIndicatorText.text = $"{CharacterPersona.CharacterName} is typing...";

        //string userId = FirebaseChatStorage.Instance.CurrentUserId;

        // Lưu tin nhắn của user lên Firestore ngay
        //FirebaseChatStorage.Instance.SaveMessage("user", userMessage);

        DeepSeekChatManager.Instance.SendMessage(
            //userId,
            "TestingUserID",
            userMessage,
            currentIntimacyLevel,
            onSuccess: (parsedReply) =>
            {
                loadingIndicatorText.text = "";
                AppendToChatDisplay($"{CharacterPersona.CharacterName}: {parsedReply.displayText}");

                // Lưu reply của AI lên Firestore
                //FirebaseChatStorage.Instance.SaveMessage("model", parsedReply.displayText);

                // Tăng điểm thân mật sau mỗi lượt trao đổi thành công
                currentIntimacyLevel = CharacterPersona.ClampIntimacy(
                    currentIntimacyLevel + CharacterPersona.CalculateIntimacyGain());
                //FirebaseChatStorage.Instance.UpdateIntimacyLevel(currentIntimacyLevel);

                // Nếu AI quyết định gửi ảnh (dựa theo tag [SEND_IMAGE: ...] đã được parse)
                if (parsedReply.hasImage)
                {
                    RequestAIImage(parsedReply.imagePromptDescription);
                }
            },
            onError: (err) =>
            {
                loadingIndicatorText.text = "";
                AppendToChatDisplay("[Error] Could not send message: " + err);
            }
        );
    }

    public void OnSendButtonPressedFridayAI()
    {
        FridayAIChat.Instance.SendMessage(userInputField.text);
    }

    #endregion


    #region GEN IMAGE METHODS

    public void GenAIImage()
    {
        FluxImageManager.Instance.GenerateImage("young woman at a beach cafe, casual lifestyle photo, natural lighting, smiling",
                                                onSuccess: (texture) => { 
                                                    aiImageDisplay.texture = texture;
                                                    Debug.Log("Image generated successfully!");
                                                },
                                                onError: (err) => Debug.LogError("Failed: " + err));
    }    

    private void RequestAIImage(string imageDescription)
    {
        // Ghép thêm style guide vào prompt để đảm bảo ảnh luôn ở mức an toàn, đời thường
        string safePrompt =
            $"{imageDescription}, casual lifestyle photo, natural lighting, safe for work, " +
            "tasteful, non-explicit, realistic photography style";

        loadingIndicatorText.text = "Sending a photo...";

        imageManager.GenerateImage(
            safePrompt,
            onSuccess: (texture) =>
            {
                loadingIndicatorText.text = "";
                aiImageDisplay.texture = texture;
                AppendToChatDisplay($"[{CharacterPersona.CharacterName} sent a photo]");

                // Lưu ảnh vào lịch sử chat trên Firestore
                // (Lưu ý: FluxImageManager hiện trả về Texture2D; nếu muốn lưu URL vào Firestore
                // để load lại sau này, cần chỉnh FluxImageManager trả thêm imageUrl ra ngoài).
                //FirebaseChatStorage.Instance.SaveMessage("model", $"[sent a photo: {imageDescription}]");
            },
            onError: (err) =>
            {
                loadingIndicatorText.text = "";
                AppendToChatDisplay("[Error] Could not generate photo: " + err);
            }
        );
    }

    #endregion


    #region LOAD IMAGE URL METHODS

    public void SetImageUrl(string url)
    {
        imgUrl = url;
    }

    public void OnLoadImageFromUrlButtonClicked()
    {
        LoadImageFromURL(
            onSuccess: () => Debug.Log("Image loaded successfully."),
            onError: (err) => Debug.LogError("Failed to load image: " + err)
        );
    }

    public void LoadImageFromURL(Action onSuccess = null, Action<string> onError = null)
    {
        if (aiImageDisplay == null)
        {
            Debug.LogError("ImageLoader: targetImage is null. Assign defaultTargetImage in Inspector or pass one explicitly.");
            onError?.Invoke("No target RawImage assigned");
            return;
        }

        if (string.IsNullOrEmpty(imgUrl))
        {
            Debug.LogError("ImageLoader: URL is empty.");
            onError?.Invoke("Empty URL");
            return;
        }    

        StartCoroutine(LoadImageCoroutine(onSuccess, onError));
    }

    private IEnumerator LoadImageCoroutine(Action onSuccess, Action<string> onError)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imgUrl))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("ImageLoader: failed to download image - " + request.error);
                onError?.Invoke(request.error);
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);

            if (texture == null)
            {
                onError?.Invoke("Downloaded texture is null");
                yield break;
            }

            aiImageDisplay.texture = texture;

            // Tự resize RawImage theo đúng tỉ lệ ảnh gốc để không bị kéo méo.
            // Giữ chiều rộng hiện tại của RectTransform, chỉ tính lại chiều cao tương ứng.
            RectTransform rect = aiImageDisplay.rectTransform;
            float aspectRatio = (float)texture.height / texture.width;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, rect.sizeDelta.x * aspectRatio);

            onSuccess?.Invoke();
        }
    }

    #endregion


    private void AppendToChatDisplay(string line)
    {
        chatDisplayText.text += "\n" + line;
    }
}