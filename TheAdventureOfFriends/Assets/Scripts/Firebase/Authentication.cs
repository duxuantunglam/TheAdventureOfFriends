using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class Authentication : MonoBehaviour
{
    public static Authentication instance { get; private set; }
    public static UserData CurrentUser { get; private set; }

    public GameObject loginPanel, signUpPanel, profilePanel, forgetPasswordPanel, notificationPanel;

    public TMP_InputField loginEmail, loginPassword, signUpEmail, signUpPassword, signUpConfirmPassword, signupUserName, forgetPassEmail;

    public TMP_Text notiTitleText, notiMessageText, profileUserNameText;

    public Toggle rememberMe;

    FirebaseAuth auth;
    FirebaseUser user;

    private DatabaseReference dbReference;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);

        if (instance == null)
        {
            instance = this;
        }

        loginPanel.SetActive(true);

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                InitializeFirebase();
            }
            else
            {
                Debug.LogError(System.String.Format("Could not resolve all Firebase dependencies: {0}", dependencyStatus));
            }
        });
    }

    public void PlayGameButton()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public void OpenPanel(string panelName)
    {
        loginPanel.SetActive(false);
        signUpPanel.SetActive(false);
        profilePanel.SetActive(false);
        forgetPasswordPanel.SetActive(false);

        switch (panelName)
        {
            case "Login":
                loginPanel.SetActive(true);
                break;

            case "SignUp":
                signUpPanel.SetActive(true);
                break;

            case "Profile":
                profilePanel.SetActive(true);
                break;

            case "ForgetPassword":
                forgetPasswordPanel.SetActive(true);
                break;

            default:
                Debug.LogWarning("Invalid panel name: " + panelName);
                break;
        }
    }

    public void LoginUser()
    {
        if (string.IsNullOrEmpty(loginEmail.text) || string.IsNullOrEmpty(loginPassword.text))
        {
            showNotificationMessage("Error", "Fields Empty! Please Input Details In All Fields");
            return;
        }

        SignInUser(loginEmail.text, loginPassword.text);
    }

    public void SignUpUser()
    {
        if (string.IsNullOrEmpty(signUpEmail.text) || string.IsNullOrEmpty(signUpPassword.text) || string.IsNullOrEmpty(signUpConfirmPassword.text) || string.IsNullOrEmpty(signupUserName.text))
        {
            showNotificationMessage("Error", "Fields Empty! Please Input Details In All Fields");
            return;
        }

        CreateUser(signUpEmail.text, signUpPassword.text, signupUserName.text);
    }

    public void ForgetPassword()
    {
        if (string.IsNullOrEmpty(forgetPassEmail.text))
        {
            showNotificationMessage("Error", "Fields Empty! Please Input Details In All Fields");
            return;
        }

        forgetPasswordSubmit(forgetPassEmail.text);
    }

    private void showNotificationMessage(string title, string message)
    {
        notiTitleText.text = "" + title;
        notiMessageText.text = "" + message;

        notificationPanel.SetActive(true);
    }

    public void CloseNotiPanel()
    {
        notiTitleText.text = "";
        notiMessageText.text = "";

        notificationPanel.SetActive(false);
    }

    public void LogOut()
    {
        auth.SignOut();
        profileUserNameText.text = "";
        OpenPanel("Login");
    }

    public void CreateUser(string email, string password, string userName)
    {
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("CreateUserWithEmailAndPasswordAsync was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("CreateUserWithEmailAndPasswordAsync encountered an error: " + task.Exception);

                foreach (Exception exception in task.Exception.Flatten().InnerExceptions)
                {
                    FirebaseException firebaseEx = exception as FirebaseException;
                    if (firebaseEx != null)
                    {
                        var errorCode = (AuthError)firebaseEx.ErrorCode;
                        showNotificationMessage("Error", GetErrorMessage(errorCode));
                    }
                }

                return;
            }

            AuthResult result = task.Result;
            Debug.LogFormat("Firebase user created successfully: {0} ({1})", result.User.DisplayName, result.User.UserId);

            UpdateUserProfile(userName);
        });
    }

    public void SignInUser(string email, string password)
    {
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("SignInWithEmailAndPasswordAsync was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("SignInWithEmailAndPasswordAsync encountered an error: " + task.Exception);

                foreach (Exception exception in task.Exception.Flatten().InnerExceptions)
                {
                    FirebaseException firebaseEx = exception as FirebaseException;
                    if (firebaseEx != null)
                    {
                        var errorCode = (AuthError)firebaseEx.ErrorCode;
                        showNotificationMessage("Error", GetErrorMessage(errorCode));
                    }
                }
                return;
            }
            AuthResult result = task.Result;
        });
    }

    void InitializeFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;

        FirebaseApp.DefaultInstance.Options.DatabaseUrl = new Uri("https://theadventureoffriends-default-rtdb.asia-southeast1.firebasedatabase.app/");

        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        auth.StateChanged += AuthStateChanged;
    }

    void AuthStateChanged(object sender, EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null
                && auth.CurrentUser.IsValid();
            if (!signedIn && user != null)
            {
                Debug.Log("Signed out " + user.UserId);
                CurrentUser = null;
                OpenPanel("Login");
            }
            user = auth.CurrentUser;
            if (signedIn)
            {
                Debug.Log("Signed in " + user.UserId);
                profileUserNameText.text = "" + user.DisplayName;
                OpenPanel("Profile");

                LoadUserDataFromRealtimeDatabase();
            }
        }
    }

    void OnDestroy()
    {
        auth.StateChanged -= AuthStateChanged;
        auth = null;
    }

    public void UpdateUserProfile(string userName)
    {
        FirebaseUser user = auth.CurrentUser;
        if (user != null)
        {
            UserProfile profile = new UserProfile
            {
                DisplayName = userName,
                PhotoUrl = new Uri("https://placehold.co/600x400"),
            };
            user.UpdateUserProfileAsync(profile).ContinueWith(task =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogError("UpdateUserProfileAsync was canceled.");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError("UpdateUserProfileAsync encountered an error: " + task.Exception);
                    return;
                }

                Debug.Log("User profile updated successfully.");

                showNotificationMessage("Alert", "Account Successfully Created!");
            });
        }
    }

    private static string GetErrorMessage(AuthError errorCode)
    {
        var message = "";
        switch (errorCode)
        {
            case AuthError.AccountExistsWithDifferentCredentials:
                message = "Account not exist!";
                break;
            case AuthError.MissingPassword:
                message = "Missing Password!";
                break;
            case AuthError.WeakPassword:
                message = "Password so weak!";
                break;
            case AuthError.WrongPassword:
                message = "Wrong Password!";
                break;
            case AuthError.EmailAlreadyInUse:
                message = "This email already in use!";
                break;
            case AuthError.InvalidEmail:
                message = "This email invalid!";
                break;
            case AuthError.MissingEmail:
                message = "Missing email!";
                break;
            default:
                message = "Invalid error!";
                break;
        }
        return message;
    }

    void forgetPasswordSubmit(string forgetPasswordEmail)
    {
        auth.SendPasswordResetEmailAsync(forgetPasswordEmail).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("SendPasswordResetEmail was canceled.");
            }
            if (task.IsFaulted)
            {
                foreach (Exception exception in task.Exception.Flatten().InnerExceptions)
                {
                    FirebaseException firebaseEx = exception as FirebaseException;
                    if (firebaseEx != null)
                    {
                        var errorCode = (AuthError)firebaseEx.ErrorCode;
                        showNotificationMessage("Error", GetErrorMessage(errorCode));
                    }
                }
            }

            showNotificationMessage("Alert", "Successfully send email for reset password!");
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
                        id = auth.CurrentUser.UserId
                    };
                    SaveUserDataToRealtimeDatabase();
                }
            }
        });
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveUserDataToRealtimeDatabase();
        }
    }

    private void OnApplicationQuit()
    {
        SaveUserDataToRealtimeDatabase();
    }
}