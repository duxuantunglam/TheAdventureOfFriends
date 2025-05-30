using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager instance { get; private set; }
    public static UserData CurrentUser { get; private set; }

    private FirebaseAuth auth;
    private FirebaseUser user;
    private DatabaseReference dbReference;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                FirebaseApp.DefaultInstance.Options.DatabaseUrl = new Uri("https://theadventureoffriends-default-rtdb.asia-southeast1.firebasedatabase.app/");
                dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                auth.StateChanged += AuthStateChanged;
                Debug.Log("Firebase Initialized");
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
            }
        });
    }

    public async Task<bool> IsUsernameTaken(string username)
    {
        var snapshot = await dbReference.Child("PlayerStats").OrderByChild("userName").EqualTo(username).GetValueAsync();
        return snapshot.Exists;
    }

    public async void CreateUser(string email, string password, string userName, Action<string> onError, Action onSuccess)
    {
        if (await IsUsernameTaken(userName))
        {
            onError?.Invoke("Username already taken!");
            return;
        }

        await auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("CreateUserWithEmailAndPasswordAsync was canceled.");
                onError?.Invoke("Creation cancelled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("CreateUserWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                foreach (Exception exception in task.Exception.Flatten().InnerExceptions)
                {
                    if (exception is FirebaseException firebaseEx)
                    {
                        var errorCode = (AuthError)firebaseEx.ErrorCode;
                        onError?.Invoke(GetErrorMessage(errorCode));
                    }
                }
                return;
            }

            UpdateUserProfile(userName, onError, onSuccess);
        });
    }

    public void SignInUser(string email, string password, Action<string> onError, Action onSuccess)
    {
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("SignInWithEmailAndPasswordAsync was canceled.");
                onError?.Invoke("Sign In cancelled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("SignInWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                foreach (Exception exception in task.Exception.Flatten().InnerExceptions)
                {
                    if (exception is FirebaseException firebaseEx)
                    {
                        var errorCode = (AuthError)firebaseEx.ErrorCode;
                        onError?.Invoke(GetErrorMessage(errorCode));
                    }
                }
                return;
            }

            if (task.IsCompleted && auth.CurrentUser != null)
            {
                UpdateLastOnlineTime(auth.CurrentUser.UserId);
                onSuccess?.Invoke();
            }
        });
    }

    public void SendPasswordResetEmail(string email, Action<string> onError, Action onSuccess)
    {
        auth.SendPasswordResetEmailAsync(email).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("SendPasswordResetEmail was canceled.");
                onError?.Invoke("Password reset email cancelled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("SendPasswordResetEmail encountered an error: " + task.Exception);
                foreach (Exception exception in task.Exception.Flatten().InnerExceptions)
                {
                    if (exception is FirebaseException firebaseEx)
                    {
                        var errorCode = (AuthError)firebaseEx.ErrorCode;
                        onError?.Invoke(GetErrorMessage(errorCode));
                    }
                }
                return;
            }
            if (task.IsCompleted)
            {
                Debug.Log("Password reset email sent successfully.");
                onSuccess?.Invoke();
            }
        });
    }

    public void LogOut()
    {
        if (auth.CurrentUser != null)
        {
            SetUserOnlineStatus(auth.CurrentUser.UserId, false);
            auth.SignOut();
            CurrentUser = null;
        }

        if (Authentication.instance != null)
            Authentication.instance.OnUserSignedOut();
    }

    private void UpdateUserProfile(string userName, Action<string> onError, Action onSuccess)
    {
        FirebaseUser user = auth.CurrentUser;
        if (user != null)
        {
            UserProfile profile = new UserProfile
            {
                DisplayName = userName,
                PhotoUrl = new Uri("https://placehold.co/600x400"),
            };
            user.UpdateUserProfileAsync(profile).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogError("UpdateUserProfileAsync was canceled.");
                    onError?.Invoke("Profile update cancelled.");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError("UpdateUserProfileAsync encountered an error: " + task.Exception);
                    onError?.Invoke("Profile update failed.");
                    return;
                }

                Debug.Log("User profile updated successfully.");
                onSuccess?.Invoke();
            });
        }
    }

    private static string GetErrorMessage(AuthError errorCode)
    {
        switch (errorCode)
        {
            case AuthError.AccountExistsWithDifferentCredentials:
                return "Account exists with different credentials!";
            case AuthError.MissingPassword:
                return "Missing Password!";
            case AuthError.WeakPassword:
                return "Password is too weak!";
            case AuthError.WrongPassword:
                return "Wrong Password!";
            case AuthError.EmailAlreadyInUse:
                return "This email is already in use!";
            case AuthError.InvalidEmail:
                return "This email is invalid!";
            case AuthError.MissingEmail:
                return "Missing email!";
            case AuthError.UserNotFound:
                return "User not found!";
            default:
                Debug.LogError($"Unhandled AuthError: {errorCode}");
                return "Authentication error!";
        }
    }

    private void AuthStateChanged(object sender, EventArgs eventArgs)
    {
        FirebaseUser prevUser = user;
        user = auth.CurrentUser;

        if (user != prevUser)
        {
            bool signedIn = user != null && user.IsValid();

            if (!signedIn && prevUser != null)
            {
                Debug.Log("Signed out " + prevUser.UserId);
                CurrentUser = null;
                SetUserOnlineStatus(prevUser.UserId, false);
                if (Authentication.instance != null)
                    Authentication.instance.OnUserSignedOut();
            }
            else if (signedIn)
            {
                Debug.Log("Signed in " + user.UserId);
                LoadUserDataFromRealtimeDatabase();
                SetUserOnlineStatus(user.UserId, true);
                SetOnDisconnectOnlineStatus(user.UserId, false);
                UpdateLastOnlineTime(user.UserId);
                if (Authentication.instance != null)
                    Authentication.instance.OnUserSignedIn(user.DisplayName);
            }
        }
    }

    private void LoadUserDataFromRealtimeDatabase()
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogWarning("Authentication: auth.CurrentUser is null. Cannot load user data.");
            return;
        }

        dbReference.Child("PlayerStats").Child(auth.CurrentUser.UserId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Could not load user data from Realtime Database: " + task.Exception);
                return;
            }
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    string json = snapshot.GetRawJsonValue();
                    UserData userData = JsonUtility.FromJson<UserData>(json);
                    Debug.Log("Authentication: User data loaded successfully.");
                    CurrentUser = userData;
                    if (auth.CurrentUser != null)
                    {
                        CurrentUser.userName = auth.CurrentUser.DisplayName ?? "Unknown";
                    }
                }
                else
                {
                    Debug.Log("Authentication: User data not found. Creating new data.");
                    CurrentUser = new UserData
                    {
                        userName = auth.CurrentUser.DisplayName ?? "Unknown",
                        id = auth.CurrentUser.UserId,
                    };
                    SaveUserDataToRealtimeDatabase();
                }
            }
        });
    }

    public void SaveUserDataToRealtimeDatabase()
    {
        if (auth.CurrentUser == null || CurrentUser == null)
        {
            Debug.LogWarning("Authentication: auth.CurrentUser or CurrentUser is null. Cannot save user data.");
            return;
        }

        string json = JsonUtility.ToJson(CurrentUser);
        dbReference.Child("PlayerStats").Child(auth.CurrentUser.UserId).SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Could not save user data to Realtime Database: " + task.Exception);
                }
                else if (task.IsCompleted)
                {
                    Debug.Log("User data saved successfully to Realtime Database.");
                }
            });
    }

    private void SetUserOnlineStatus(string userId, bool isOnline)
    {
        if (dbReference == null || string.IsNullOrEmpty(userId))
        {
            Debug.LogError("Database reference or userId is null!");
            return;
        }

        dbReference.Child("PlayerStats").Child("isOnline").SetValueAsync(isOnline).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"Failed to set online status for user {userId}: {task.Exception}");
            }
            else if (task.IsCompleted)
            {
                Debug.Log($"User {userId} online status set to {isOnline}");
            }
        });
    }

    private void SetOnDisconnectOnlineStatus(string userId, bool statusOnDisconnect)
    {
        if (dbReference == null || string.IsNullOrEmpty(userId))
        {
            Debug.LogError("Database reference or userId is null!");
            return;
        }

        dbReference.Child("PlayerStats").Child("isOnline").OnDisconnect().SetValue(statusOnDisconnect).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"Failed to set onDisconnect status for user {userId}: {task.Exception}");
            }
            else if (task.IsCompleted)
            {
                Debug.Log($"User {userId} onDisconnect status set to {statusOnDisconnect}");
            }
        });
    }

    private void UpdateLastOnlineTime(string userId)
    {
        if (dbReference == null || string.IsNullOrEmpty(userId))
        {
            Debug.LogError("Database reference or userId is null!");
            return;
        }

        dbReference.Child("PlayerStats").Child("lastOnlineTime").SetValueAsync(ServerValue.Timestamp).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"Failed to update last online time for user {userId}: {task.Exception}");
            }
            else if (task.IsCompleted)
            {
                Debug.Log($"User {userId} last online time updated");
                UpdatePlayTimeInDay();
            }
        });
    }

    private void UpdatePlayTimeInDay()
    {
        if (CurrentUser != null)
        {
            int slot = DateTime.Now.Hour / 3;
            CurrentUser.playTimeInDay[slot]++;
            dbReference.Child("PlayerStats").Child(auth.CurrentUser.UserId).Child("playTimeInDay").Child(slot.ToString()).SetValueAsync(CurrentUser.playTimeInDay[slot]);
        }
    }

    private void OnDestroy()
    {
        if (auth != null && auth.CurrentUser != null)
        {
            SetUserOnlineStatus(auth.CurrentUser.UserId, false);
            dbReference.Child("PlayerStats").Child(auth.CurrentUser.UserId).Child("isOnline").OnDisconnect().Cancel();
        }
        if (auth != null)
        {
            auth.StateChanged -= AuthStateChanged;
            auth = null;
        }
    }
}