// FirebaseChatStorage.cs
// Yêu cầu: cài Firebase Unity SDK (Firebase Auth + Firebase Firestore) qua Package Manager
// hoặc .unitypackage từ https://firebase.google.com/docs/unity/setup
//
// Gắn script này vào 1 GameObject (ví dụ "FirebaseManager"), đặt càng sớm càng tốt trong scene đầu tiên.

/*
using System;
using System.Collections.Generic;
using System.Linq;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine;

[FirestoreData]
public class UserProfileData
{
    [FirestoreProperty]
    public int IntimacyLevel { get; set; }

    [FirestoreProperty]
    public long CreatedAt { get; set; }

    [FirestoreProperty]
    public long LastActive { get; set; }
}

[FirestoreData]
public class MessageData
{
    [FirestoreProperty]
    public string Role { get; set; } // "user" hoặc "model"

    [FirestoreProperty]
    public string Text { get; set; }

    [FirestoreProperty]
    public string ImageUrl { get; set; } // null nếu không có ảnh

    [FirestoreProperty]
    public long Timestamp { get; set; }
}

public class FirebaseChatStorage : MonoBehaviour
{
    public static FirebaseChatStorage Instance { get; private set; }

    private FirebaseFirestore db;
    private FirebaseAuth auth;
    public string CurrentUserId { get; private set; }

    public bool IsReady { get; private set; } = false;

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

    /// <summary>
    /// Gọi hàm này ở màn hình đầu tiên (splash/loading screen) trước khi vào chat.
    /// </summary>
    public void Initialize(Action onReady, Action<string> onError)
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result != DependencyStatus.Available)
            {
                onError?.Invoke("Firebase dependency error: " + task.Result);
                return;
            }

            db = FirebaseFirestore.DefaultInstance;
            auth = FirebaseAuth.DefaultInstance;

            // Đăng nhập ẩn danh - đơn giản cho MVP.
            // Sau này có thể nâng cấp lên email/Google/Apple Sign-In và link tài khoản anonymous đó.
            auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(authTask =>
            {
                if (authTask.IsCanceled || authTask.IsFaulted)
                {
                    onError?.Invoke("Auth failed: " + authTask.Exception);
                    return;
                }

                CurrentUserId = authTask.Result.User.UserId;
                IsReady = true;

                EnsureUserProfileExists(onReady, onError);
            });
        });
    }

    private void EnsureUserProfileExists(Action onReady, Action<string> onError)
    {
        DocumentReference userDoc = db.Collection("users").Document(CurrentUserId);

        userDoc.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                onError?.Invoke("Failed to fetch user profile: " + task.Exception);
                return;
            }

            DocumentSnapshot snapshot = task.Result;

            if (!snapshot.Exists)
            {
                var newProfile = new UserProfileData
                {
                    IntimacyLevel = 0,
                    CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    LastActive = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                userDoc.SetAsync(newProfile).ContinueWithOnMainThread(setTask =>
                {
                    onReady?.Invoke();
                });
            }
            else
            {
                onReady?.Invoke();
            }
        });
    }

    /// <summary>
    /// Lấy intimacy level hiện tại của user.
    /// </summary>
    public void GetIntimacyLevel(Action<int> onSuccess, Action<string> onError)
    {
        DocumentReference userDoc = db.Collection("users").Document(CurrentUserId);

        userDoc.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || !task.Result.Exists)
            {
                onError?.Invoke("Failed to get intimacy level");
                return;
            }

            UserProfileData profile = task.Result.ConvertTo<UserProfileData>();
            onSuccess?.Invoke(profile.IntimacyLevel);
        });
    }

    /// <summary>
    /// Cập nhật intimacy level mới (sau mỗi lượt chat, dùng CharacterPersona.ClampIntimacy để giới hạn 0-100).
    /// </summary>
    public void UpdateIntimacyLevel(int newLevel)
    {
        DocumentReference userDoc = db.Collection("users").Document(CurrentUserId);

        var updates = new Dictionary<string, object>
        {
            { "IntimacyLevel", newLevel },
            { "LastActive", DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
        };

        userDoc.UpdateAsync(updates);
    }

    /// <summary>
    /// Lưu 1 tin nhắn vào subcollection messages của user.
    /// </summary>
    public void SaveMessage(string role, string text, string imageUrl = null)
    {
        var message = new MessageData
        {
            Role = role,
            Text = text,
            ImageUrl = imageUrl,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        db.Collection("users").Document(CurrentUserId)
          .Collection("messages")
          .AddAsync(message);
    }

    /// <summary>
    /// Tải lịch sử chat gần nhất (mặc định 50 tin nhắn gần nhất, sắp xếp theo thời gian tăng dần).
    /// Dùng để hiển thị lại UI khi user mở lại app, và để nạp vào GeminiChatManager.LoadHistory().
    /// </summary>
    public void LoadRecentMessages(int limit, Action<List<MessageData>> onSuccess, Action<string> onError)
    {
        db.Collection("users").Document(CurrentUserId)
          .Collection("messages")
          .OrderByDescending("Timestamp")
          .Limit(limit)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted)
              {
                  onError?.Invoke("Failed to load messages: " + task.Exception);
                  return;
              }

              List<MessageData> messages = task.Result.Documents
                  .Select(doc => doc.ConvertTo<MessageData>())
                  .OrderBy(m => m.Timestamp) // đảo lại thành thứ tự tăng dần (cũ -> mới)
                  .ToList();

              onSuccess?.Invoke(messages);
          });
    }
}
*/
