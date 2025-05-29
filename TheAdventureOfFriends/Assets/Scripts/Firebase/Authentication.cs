using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Authentication : MonoBehaviour
{
    public static Authentication instance { get; private set; }

    public GameObject loginPanel, signUpPanel, profilePanel, forgetPasswordPanel, notificationPanel;
    public TMP_InputField loginEmail, loginPassword, signUpEmail, signUpPassword, signUpConfirmPassword, signupUserName, forgetPassEmail;
    public TMP_Text notiTitleText, notiMessageText, profileUserNameText;
    public Toggle rememberMe;

    private GameObject currentPanel;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (FirebaseManager.instance != null && FirebaseManager.CurrentUser != null)
        {
            OnUserSignedIn(FirebaseManager.CurrentUser.userName);
        }
        else
        {
            OpenPanel("Login");
        }
    }

    public void PlayGameButton()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void OpenPanel(string panelName)
    {
        if (currentPanel != null)
            currentPanel.SetActive(false);

        switch (panelName)
        {
            case "Login":
                currentPanel = loginPanel;
                break;
            case "SignUp":
                currentPanel = signUpPanel;
                break;
            case "Profile":
                currentPanel = profilePanel;
                break;
            case "ForgetPassword":
                currentPanel = forgetPasswordPanel;
                break;
            default:
                Debug.LogWarning("Invalid panel name: " + panelName);
                return;
        }
        currentPanel.SetActive(true);
    }

    public void LoginUser()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            ShowNotificationMessage("Error", "No internet connection!");
            return;
        }

        if (string.IsNullOrEmpty(loginEmail.text) || string.IsNullOrEmpty(loginPassword.text))
        {
            ShowNotificationMessage("Error", "Fields Empty! Please Input Details In All Fields");
            return;
        }

        FirebaseManager.instance.SignInUser(loginEmail.text, loginPassword.text,
            error => ShowNotificationMessage("Error", error),
            () => loginPassword.text = "");
    }

    public void SignUpUser()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            ShowNotificationMessage("Error", "No internet connection!");
            return;
        }

        if (string.IsNullOrEmpty(signUpEmail.text) || string.IsNullOrEmpty(signUpPassword.text) ||
            string.IsNullOrEmpty(signUpConfirmPassword.text) || string.IsNullOrEmpty(signupUserName.text))
        {
            ShowNotificationMessage("Error", "Fields Empty! Please Input Details In All Fields");
            return;
        }

        if (signUpPassword.text != signUpConfirmPassword.text)
        {
            ShowNotificationMessage("Error", "Passwords do not match!");
            return;
        }

        FirebaseManager.instance.CreateUser(signUpEmail.text, signUpPassword.text, signupUserName.text,
            error => ShowNotificationMessage("Error", error),
            () =>
            {
                signUpPassword.text = "";
                signUpConfirmPassword.text = "";
                ShowNotificationMessage("Alert", "Account Successfully Created! Please login.");
                OpenPanel("Login");
            });
    }

    public void ForgetPassword()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            ShowNotificationMessage("Error", "No internet connection!");
            return;
        }

        if (string.IsNullOrEmpty(forgetPassEmail.text))
        {
            ShowNotificationMessage("Error", "Fields Empty! Please Input Details In All Fields");
            return;
        }

        FirebaseManager.instance.SendPasswordResetEmail(forgetPassEmail.text,
            error => ShowNotificationMessage("Error", error),
            () =>
            {
                ShowNotificationMessage("Alert", "Successfully sent email for reset password!");
                OpenPanel("Login");
            });
    }

    public void LogOut()
    {
        FirebaseManager.instance.LogOut();
    }

    private void ShowNotificationMessage(string title, string message)
    {
        notiTitleText.text = title;
        notiMessageText.text = message;
        notificationPanel.SetActive(true);
    }

    public void CloseNotiPanel()
    {
        notiTitleText.text = "";
        notiMessageText.text = "";
        notificationPanel.SetActive(false);
    }

    public void OnUserSignedIn(string displayName)
    {
        profileUserNameText.text = $"Welcome {displayName}!";
        OpenPanel("Profile");
    }

    public void OnUserSignedOut()
    {
        profileUserNameText.text = "";
        OpenPanel("Login");
    }
}