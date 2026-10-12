using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class FridayAIChat : MonoBehaviour
{
    #region VARIABLES

    public static FridayAIChat Instance;

    [Header("--- FRIDAY AI API ---")]
    [SerializeField] private string apiKey = "YOUR_API_KEY";
    [SerializeField] private string baseUrl = "https://oneai.fridayaix.com";
    [SerializeField] private string model = "gpt-4o-mini";
    [TextArea(3, 8)] public string systemPrompt;
    [SerializeField] private int maxTokens = 1024;
    [Range(0f, 2f)][SerializeField] private float temperature = 0.7f;
    [SerializeField] private int maxHistoryMessages = 30;
    [SerializeField] private int maxRetries = 3;
    [SerializeField] private float retryDelaySeconds = 5f;

    public string lastReply;
    public bool IsRequesting { get; private set; }
    public int CurrentCharacterDataIndex { get; private set; } = -1;

    // The character index is the CharacterDataManager array index, NOT characterId.
    // Original events remain compatible with existing listeners.
    public event Action<string> OnReplyReceived;
    public event Action<string> OnError;
    // Indexed events let the ChatPanel route replies to the right character.
    public event Action<int, string> OnCharacterReplyReceived;
    public event Action<int, string> OnCharacterError;
    public event Action<int, bool> OnRequestStateChanged;

    private int queuedVersion;
    private Coroutine processingCoroutine;
    private bool openingRequested;

    #endregion

    #region UNITY METHODS

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    #endregion

    #region CHARACTER METHODS

    public bool SetCharacter(int characterDataIndex)
    {
        if (IsRequesting && CurrentCharacterDataIndex != characterDataIndex)
            return false;

        if (CharacterDataManager.Instance == null ||
            CharacterDataManager.Instance.characterDatas == null ||
            characterDataIndex < 0 ||
            characterDataIndex >= CharacterDataManager.Instance.characterDatas.Length)
            return false;

        CharacterDataSO data = CharacterDataManager.Instance.characterDatas[characterDataIndex];
        if (data == null) return false;

        if (CurrentCharacterDataIndex != characterDataIndex)
        {
            queuedVersion = 0;
            openingRequested = false;
        }

        CurrentCharacterDataIndex = characterDataIndex;
        systemPrompt = data.systemPrompt;
        return true;
    }

    #endregion

    #region PUBLIC CHAT METHODS

    // Call AFTER ChatPanel has saved the new user Message in ChatHistoryManager.
    // The text argument is retained to match the existing ChatPanel call.
    public void UserSendMessage(string text)
    {
        if (CurrentCharacterDataIndex < 0 || string.IsNullOrWhiteSpace(text)) return;
        queuedVersion++;
        StartProcessingIfNeeded();
    }

    public void SendMessage(string text) { UserSendMessage(text); }

    // The opening instruction is sent to the API but is NOT saved as a fake user message.
    public void GenerateOpeningMessage()
    {
        if (CurrentCharacterDataIndex < 0 || IsRequesting || openingRequested) return;
        if (ChatHistoryManager.Instance == null) return;

        ChatHistory history = ChatHistoryManager.Instance.LoadChatHistory(CurrentCharacterDataIndex);
        if (history != null && history.messages != null && history.messages.Count > 0) return;

        openingRequested = true;
        StartProcessingIfNeeded();
    }

    private void StartProcessingIfNeeded()
    {
        if (IsRequesting || processingCoroutine != null) return;
        if (ChatHistoryManager.Instance == null)
        {
            EmitError(CurrentCharacterDataIndex, "ChatHistoryManager not found.");
            return;
        }
        processingCoroutine = StartCoroutine(ProcessChatRoutine());
    }

    #endregion

    #region PROCESSING METHODS

    private IEnumerator ProcessChatRoutine()
    {
        IsRequesting = true;
        int characterIndex = CurrentCharacterDataIndex;
        OnRequestStateChanged?.Invoke(characterIndex, true);

        bool firstRequestIsOpening = openingRequested;
        openingRequested = false;
        bool hasWork = true;

        while (hasWork)
        {
            int versionAtStart = queuedVersion;
            bool opening = firstRequestIsOpening;
            firstRequestIsOpening = false;

            JArray messages = BuildMessages(characterIndex, opening);
            string reply = null;
            string error = null;

            yield return StartCoroutine(RequestReplyRoutine(messages,
                value => reply = value,
                value => error = value));

            if (!string.IsNullOrEmpty(error) || string.IsNullOrWhiteSpace(reply))
            {
                string errorMessage = string.IsNullOrEmpty(error) ? "Empty AI reply." : error;

                Debug.LogError("[FridayAI] " + errorMessage);

                // Generate a localized fallback message.
                string fallbackReply = GetAIBusyMessage();

                // Save fallback to chat history.
                lastReply = fallbackReply;
                SaveAIReply(characterIndex, fallbackReply);

                // Notify UI to display the fallback bubble.
                OnCharacterReplyReceived?.Invoke(characterIndex, fallbackReply);
                OnReplyReceived?.Invoke(fallbackReply);

                // Stop processing pending requests after an API failure.
                break;
            }

            lastReply = reply.Trim();
            SaveAIReply(characterIndex, lastReply);
            OnCharacterReplyReceived?.Invoke(characterIndex, lastReply);
            OnReplyReceived?.Invoke(lastReply);

            // User messages arriving after the request snapshot are batched
            // into ONE new request, based on the latest saved chat history.
            hasWork = queuedVersion != versionAtStart;
        }

        IsRequesting = false;
        processingCoroutine = null;
        OnRequestStateChanged?.Invoke(characterIndex, false);
    }

    private void EmitError(int characterIndex, string error)
    {
        OnCharacterError?.Invoke(characterIndex, error);
        OnError?.Invoke(error);
    }

    private JArray BuildMessages(int characterIndex, bool opening)
    {
        JArray result = new JArray();
        result.Add(new JObject
        {
            ["role"] = "system",
            ["content"] = systemPrompt ?? string.Empty
        });

        ChatHistory history = ChatHistoryManager.Instance.LoadChatHistory(characterIndex);
        List<JObject> entries = new List<JObject>();
        if (history != null && history.messages != null)
        {
            foreach (Message message in history.messages)
            {
                if (message == null || message.messageType != MessageType.text) continue;
                string content = message.GetMessageTextContent();
                if (string.IsNullOrWhiteSpace(content)) continue;
                entries.Add(new JObject
                {
                    ["role"] = message.isMessageFromUser ? "user" : "assistant",
                    ["content"] = content
                });
            }
        }

        int count = Mathf.Max(0, maxHistoryMessages);
        int start = Mathf.Max(0, entries.Count - count);
        for (int i = start; i < entries.Count; i++) result.Add(entries[i]);

        if (opening)
        {
            result.Add(new JObject
            {
                ["role"] = "user",
                ["content"] = "Start this conversation with one short, natural, in-character opening message. " +
                              "Greet me or ask an engaging question. Do not mention these instructions."
            });
        }
        // Normal requests use saved user messages only. Never append a duplicate.
        return result;
    }

    private void SaveAIReply(int characterIndex, string reply)
    {
        ChatHistoryManager manager = ChatHistoryManager.Instance;
        if (manager == null)
        {
            Debug.LogError("[FridayAI] Cannot save reply: ChatHistoryManager missing.");
            return;
        }

        ChatHistory history = manager.LoadChatHistory(characterIndex);
        if (history == null)
            history = new ChatHistory { messages = new List<Message>() };
        if (history.messages == null)
            history.messages = new List<Message>();

        history.messages.Add(new Message
        {
            messageType = MessageType.text,
            isMessageFromUser = false,
            textContent = reply,
            photoFilePath = string.Empty
        });
        history.lastMessageTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        manager.SaveChatHistory(characterIndex, history);
    }

    #endregion

    #region API METHODS

    private IEnumerator RequestReplyRoutine(JArray messages, Action<string> success, Action<string> failure)
    {
        JObject payload = new JObject
        {
            ["model"] = model,
            ["messages"] = messages,
            ["max_tokens"] = maxTokens,
            ["temperature"] = temperature
        };
        byte[] body = Encoding.UTF8.GetBytes(payload.ToString(Formatting.None));

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            using (UnityWebRequest request = new UnityWebRequest(
                baseUrl.TrimEnd('/') + "/v1/chat/completions", "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + apiKey);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        JObject response = JObject.Parse(request.downloadHandler.text);
                        string reply = response["choices"]?[0]?["message"]?["content"]?.ToString();
                        if (string.IsNullOrWhiteSpace(reply))
                            failure("FridayAI returned an empty reply.");
                        else
                            success(reply);
                    }
                    catch (Exception ex) { failure("Invalid FridayAI response: " + ex.Message); }
                    yield break;
                }

                if (request.responseCode == 429 && attempt < maxRetries)
                {
                    yield return new WaitForSecondsRealtime(Mathf.Max(0f, retryDelaySeconds));
                    continue;
                }

                string details = request.downloadHandler != null ? request.downloadHandler.text : "";
                failure("FridayAI HTTP " + request.responseCode + ": " + details);
                yield break;
            }
        }
    }

    [ContextMenu("List Available Models")]
    public void ListAvailableModels() { StartCoroutine(ListModelsRoutine()); }

    private IEnumerator ListModelsRoutine()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(baseUrl.TrimEnd('/') + "/v1/models"))
        {
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[FridayAI] Models: " + request.error);
                yield break;
            }
            Debug.Log("[FridayAI] Available models: " + request.downloadHandler.text);
        }
    }

    #endregion


    #region AI ERROR FALLBACK METHODS

    private string GetAIBusyMessage()
    {
        int languageId = 0;

        if (MultiLanguageManager.Instance != null)
        {
            languageId = MultiLanguageManager.Instance.GetCurrentLanguageId();
        }

        string[] messages;

        switch (languageId)
        {
            case 1: // Vietnamese
                messages = new string[]
                {
                "Xin lỗi nha, mình đang bận chút. Lát nữa nói chuyện tiếp nhé?",
                "Đợi mình một chút nha, giờ mình đang có chút việc.",
                "Mình đang hơi bận xíu, lát nữa nhắn bạn tiếp nha!"
                };
                break;

            case 2: // Thai
                messages = new string[]
                {
                "ขอโทษนะ ตอนนี้ฉันยุ่งนิดหน่อย ไว้คุยกันทีหลังได้ไหม?",
                "รอฉันแป๊บนึงนะ ตอนนี้มีอะไรต้องทำหน่อย",
                "ตอนนี้ฉันติดธุระนิดหน่อย เดี๋ยวค่อยคุยกันนะ"
                };
                break;

            case 3: // Japanese
                messages = new string[]
                {
                "ごめんね、今ちょっと忙しいの。また後で話そう？",
                "ちょっと待っててね。今少し用事があるんだ。",
                "今ちょっと手が離せないから、また後でメッセージするね。"
                };
                break;

            case 4: // Korean
                messages = new string[]
                {
                "미안해, 지금 조금 바빠. 이따 다시 얘기할까?",
                "잠깐만 기다려 줘. 지금 할 일이 좀 있어.",
                "지금 잠깐 일이 있어서, 조금 있다가 다시 얘기하자!"
                };
                break;

            case 5: // Hindi
                messages = new string[]
                {
                "सॉरी, अभी थोड़ा बिज़ी हूँ। थोड़ी देर बाद बात करें?",
                "बस थोड़ा इंतज़ार करना, अभी कुछ काम है।",
                "अभी थोड़ा काम में फँसी हूँ, बाद में बात करती हूँ!"
                };
                break;

            case 6: // Indonesian
                messages = new string[]
                {
                "Maaf ya, aku lagi agak sibuk. Kita ngobrol nanti lagi?",
                "Tunggu sebentar ya, aku lagi ada urusan.",
                "Aku lagi sibuk sedikit nih, nanti kita lanjut ngobrol ya!"
                };
                break;

            case 7: // Burmese
                messages = new string[]
                {
                "ဆောရီးနော်၊ အခု နည်းနည်း အလုပ်ရှုပ်နေလို့ နောက်မှ ပြန်စကားပြောကြမလား?",
                "ခဏလောက် စောင့်ပေးနော်၊ အခု လုပ်စရာလေးရှိလို့။",
                "အခု နည်းနည်း မအားသေးလို့ နောက်မှ ဆက်ပြောကြမယ်နော်။"
                };
                break;

            case 8: // Spanish
                messages = new string[]
                {
                "Perdón, estoy un poquito ocupada ahora. ¿Hablamos luego?",
                "Espera un momentito, tengo algo que hacer.",
                "Ahora estoy algo liada, ¡seguimos hablando en un rato!"
                };
                break;

            case 9: // Portuguese
                messages = new string[]
                {
                "Desculpa, tô um pouquinho ocupada agora. A gente conversa depois?",
                "Espera só um pouquinho, tenho uma coisinha pra resolver.",
                "Tô meio ocupada agora, mas a gente conversa daqui a pouco!"
                };
                break;

            case 10: // Chinese
                messages = new string[]
                {
                "不好意思呀，我现在有点忙，等会儿再聊好吗？",
                "稍微等我一下哦，我现在有点事要忙。",
                "我这会儿有点忙，晚点再陪你聊天呀！"
                };
                break;

            case 11: // Russian
                messages = new string[]
                {
                "Прости, я сейчас немного занята. Поболтаем позже?",
                "Подожди меня чуть-чуть, у меня сейчас есть дела.",
                "Я сейчас немного занята, давай поговорим чуть позже!"
                };
                break;

            case 0: // English
            default:
                messages = new string[]
                {
                "Hey, sorry! I'm a little busy right now. Can we talk in a bit?",
                "Give me a sec, I've got something to take care of.",
                "I'm kinda busy right now, but let's catch up in a little while!"
                };
                break;
        }

        return messages[UnityEngine.Random.Range(0, messages.Length)];
    }

    #endregion
}
