using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Authentication : MonoBehaviour
{
    public static Authentication instance { get; private set; }

    public GameObject loginPanel, signUpPanel, profilePanel, forgetPasswordPanel, notificationPanel;

    public TMP_InputField loginEmail, loginPassword, signUpEmail, signUpPassword, signUpConfirmPassword, signupUserName, forgetPassEmail;

    public TMP_Text notiTitleText, notiMessageText, profileUserNameText;

    public Toggle rememberMe;

    Firebase.Auth.FirebaseAuth auth;
    Firebase.Auth.FirebaseUser user;

    bool isSignIn = false;

    private void Start()
    {
        if (instance == null)
        {
            instance = this;
        }

        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                InitializeFirebase();
            }
            else
            {
                Debug.LogError(System.String.Format(
                  "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
            }
        });
    }

    [SerializeField] private GameObject multiplayerInterface;
    [SerializeField] private GameObject grid;
    [SerializeField] private GameObject menuCharacter;
    [SerializeField] private GameObject gameModeUI;

    public void MultiplayerButtonClicked()
    {
        if (grid != null)
        {
            grid.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Grid object not found!");
        }

        if (menuCharacter != null)
        {
            menuCharacter.SetActive(false);
        }
        else
        {
            Debug.LogWarning("MenuCharacter object not found!");
        }

        if (gameModeUI != null)
        {
            gameModeUI.SetActive(false);
        }
        else
        {
            Debug.LogWarning("GameModeUI object not found!");
        }

        if (multiplayerInterface != null)
        {
            multiplayerInterface.SetActive(true);
        }
        else
        {
            Debug.LogWarning("MultiplayerInterface object not found!");
        }

        if (!isSignIn)
        {
            loginPanel.SetActive(true);
        }
        else
        {
            grid.SetActive(true);
            menuCharacter.SetActive(true);
        }
    }

    public void BackButtonClicked()
    {
        if (multiplayerInterface != null)
        {
            multiplayerInterface.SetActive(false);
        }
        else
        {
            Debug.LogWarning("MultiplayerInterface object not found!");
        }

        if (grid != null)
        {
            grid.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Grid object not found!");
        }

        if (menuCharacter != null)
        {
            menuCharacter.SetActive(true);
        }
        else
        {
            Debug.LogWarning("MenuCharacter object not found!");
        }

        if (gameModeUI != null)
        {
            gameModeUI.SetActive(true);
        }
        else
        {
            Debug.LogWarning("GameModeUI object not found!");
        }
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
                if (grid != null)
                {
                    grid.SetActive(true);
                }
                else
                {
                    Debug.LogWarning("Grid object not found!");
                }

                if (menuCharacter != null)
                {
                    menuCharacter.SetActive(true);
                }
                else
                {
                    Debug.LogWarning("MenuCharacter object not found!");
                }
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
                    Firebase.FirebaseException firebaseEx = exception as Firebase.FirebaseException;
                    if (firebaseEx != null)
                    {
                        var errorCode = (AuthError)firebaseEx.ErrorCode;
                        showNotificationMessage("Error", GetErrorMessage(errorCode));
                    }
                }

                return;
            }

            Firebase.Auth.AuthResult result = task.Result;
            Debug.LogFormat("Firebase user created successfully: {0} ({1})",
                result.User.DisplayName, result.User.UserId);

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
                    Firebase.FirebaseException firebaseEx = exception as Firebase.FirebaseException;
                    if (firebaseEx != null)
                    {
                        var errorCode = (AuthError)firebaseEx.ErrorCode;
                        showNotificationMessage("Error", GetErrorMessage(errorCode));
                    }
                }

                return;
            }

            Firebase.Auth.AuthResult result = task.Result;
            Debug.LogFormat("User signed in successfully: {0} ({1})",
                result.User.DisplayName, result.User.UserId);

            profileUserNameText.text = "" + result.User.DisplayName;
            OpenPanel("Profile");
        });
    }

    void InitializeFirebase()
    {
        auth = Firebase.Auth.FirebaseAuth.DefaultInstance;

        FirebaseApp.DefaultInstance.Options.DatabaseUrl = new Uri("https://pixeladventureonline-default-rtdb.asia-southeast1.firebasedatabase.app/");

        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);
    }

    void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null
                && auth.CurrentUser.IsValid();
            if (!signedIn && user != null)
            {
                Debug.Log("Signed out " + user.UserId);
            }
            user = auth.CurrentUser;
            if (signedIn)
            {
                Debug.Log("Signed in " + user.UserId);
                isSignIn = true;
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
        Firebase.Auth.FirebaseUser user = auth.CurrentUser;
        if (user != null)
        {
            Firebase.Auth.UserProfile profile = new Firebase.Auth.UserProfile
            {
                DisplayName = userName,
                PhotoUrl = new System.Uri("https://placehold.co/600x400"),
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

    bool isSigned = false;
    void Update()
    {
        if (isSignIn)
        {
            if (!isSigned)
            {
                isSigned = true;
                profileUserNameText.text = "" + user.DisplayName;
                OpenPanel("Profile");
            }
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
                    Firebase.FirebaseException firebaseEx = exception as Firebase.FirebaseException;
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
}