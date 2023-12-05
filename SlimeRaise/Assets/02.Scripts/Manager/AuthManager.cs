using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UserProfile = Firebase.Auth.UserProfile;

public class AuthManager : MonoBehaviour
{
    [Header("Firebase")]
    public FirebaseAuth auth;
    public FirebaseUser user;
    public DatabaseReference dbref;
    [Header("Login")]
    public TMP_InputField emailLoginField;
    public TMP_InputField passwordLoginField;
    public TMP_Text warningLoginText;

    [Header("Register")]
    public TMP_InputField emailRegisterField;
    public TMP_InputField usernameRegisterField;
    public TMP_InputField passwordRegisterField;
    public TMP_InputField passwordCheckRegisterField;
    public TMP_Text warningRegisterText;


    public TMP_Text userNameText;
    public Button dayRewardBtn;

    private string loadLastLogin = "";
    private string loadLastReward = "";
    private void Awake()
    {
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                // Create and hold a reference to your FirebaseApp,
                // where app is a Firebase.FirebaseApp property of your application class.
                FirebaseApp app = Firebase.FirebaseApp.DefaultInstance;
                auth = FirebaseAuth.DefaultInstance;
                dbref = FirebaseDatabase.DefaultInstance.RootReference;
                // Set a flag here to indicate whether Firebase is ready to use by your app.
            }
            else
            {
                UnityEngine.Debug.LogError(System.String.Format(
                  "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                // Firebase Unity SDK is not safe to use here.
            }
        });
    }

    private void HandleTimeValueChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }
        StartCoroutine(LoadLastLogin());
        StartCoroutine(LoadLastReward());
    }

    private IEnumerator Register(string email, string password, string username)
    {
        if (username == "")
        {
            warningRegisterText.text = "Missing Username";
        }
        else if (passwordRegisterField.text != passwordCheckRegisterField.text)
        {
            warningRegisterText.text = "Password Does not Match!";
        }
        else
        {
            var task = auth.CreateUserWithEmailAndPasswordAsync(email, password);
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.Exception != null)
            {
                Debug.LogWarning($"Failed with {task.Exception}");
                FirebaseException firebaseEx = task.Exception.GetBaseException() as FirebaseException;
                AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
                string message = "RegisterFailed!";
                switch (errorCode)
                {
                    case AuthError.MissingEmail:
                        message = "Missing Email";
                        break;
                    case AuthError.MissingPassword:
                        message = "Missing Password";
                        break;
                    case AuthError.WeakPassword:
                        message = "Weak Password";
                        break;
                    case AuthError.EmailAlreadyInUse:
                        message = "Email Already In Use";
                        break;
                }
                warningRegisterText.text = message;
            }
            else
            {
                user = task.Result.User;
                if (user != null)
                {
                    user = auth.CurrentUser;
                    if (user != null)
                    {
                        UserProfile profile = new UserProfile { DisplayName = username };
                        var profileTask = user.UpdateUserProfileAsync(profile);
                        yield return new WaitUntil(() => profileTask.IsCompleted);
                        if (profileTask.Exception != null)
                        {
                            Debug.LogWarning($"Failed to Register Userprofile:{profileTask.Exception}");
                        }
                        else
                        {
                            UIManager.Instance.LoginPanel();
                            Debug.Log($"User Profile Updated Successfully");
                            warningRegisterText.text = "";

                            StartCoroutine(SaveUsername());
                            StartCoroutine(InitRewardLogin());
                        }
                    }
                }
            }
        }
    }

    public void RegisterButton()
    {
        StartCoroutine(Register(emailRegisterField.text, passwordRegisterField.text, usernameRegisterField.text));
    }

    private IEnumerator Login(string email, string password)
    {
        var task = auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => task.IsCompleted);
        if (task.Exception != null)
        {
            Debug.LogWarning($"Failed with {task.Exception}");
            FirebaseException firebaseEx = task.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
            string message = "Login Failed!";
            switch (errorCode)
            {
                case AuthError.MissingEmail:
                    message = "Missing Email";
                    break;
                case AuthError.MissingPassword:
                    message = "Missing Password";
                    break;
                case AuthError.WeakPassword:
                    message = "Wrong Password";
                    break;
                case AuthError.EmailAlreadyInUse:
                    message = "Invalid Email";
                    break;
                case AuthError.UserNotFound:
                    message = "Account does not Exist";
                    break;
            }
            warningLoginText.text = message;
        }
        else
        {
            user = task.Result.User;
            Debug.Log($"User Signed in Successfully: {user.Email}, {user.UserId}");
            warningLoginText.text = "";
            UIManager.Instance.CloseLogin();

            StartCoroutine(SaveDate());
            StartCoroutine(LoadUsername());
            FirebaseDatabase.DefaultInstance.GetReference("RewardLogin").ValueChanged += HandleTimeValueChanged;
        }
    }

    public void LoginButton()
    {
        StartCoroutine(Login(emailLoginField.text, passwordLoginField.text));
    }

    private IEnumerator SaveUsername()
    {
        var DBTask = dbref.Child("users").Child(user.UserId).Child("UserName").SetValueAsync(usernameRegisterField.text);
        yield return new WaitUntil(() => DBTask.IsCompleted);
        if (DBTask.Exception != null)
        {
            Debug.LogWarning($"Failed to Save task with {DBTask.Exception}");
        }
        else
        {
            Debug.Log("UserName Saved");
        }
    }

    private IEnumerator InitRewardLogin()
    {
        var DBTask = dbref.Child("users").Child(user.UserId).Child("RewardLogin").SetValueAsync("00000000000000");
        yield return new WaitUntil(() => DBTask.IsCompleted);
        if (DBTask.Exception != null)
        {
            Debug.LogWarning($"Failed to Save task with {DBTask.Exception}");
        }
        else
        {
            Debug.Log("UserName Saved");
        }
    }

    private IEnumerator SaveDate()
    {
        var DBTask = dbref.Child("users").Child(user.UserId).Child("LastLogin").SetValueAsync(DateTime.Now.ToString("yyyyMMddHHmmss"));
        yield return new WaitUntil(() => DBTask.IsCompleted);
        if (DBTask.Exception != null)
        {
            Debug.LogWarning($"Failed to Save task with {DBTask.Exception}");
        }
        else
        {
            Debug.Log("Date Saved");
        }
    }

    private IEnumerator LoadUsername()
    {
        var DBTask = dbref.Child("users").Child(user.UserId).Child("UserName").GetValueAsync();
        yield return new WaitUntil(() => DBTask.IsCompleted);
        if (DBTask.Exception != null)
        {
            Debug.LogWarning($"Failed to Save task with {DBTask.Exception}");
        }
        else
        {
            DataSnapshot snapshot = DBTask.Result;
            Debug.Log("Load Complete");
            userNameText.text = $"{snapshot.Value}";
        }
    }

    private IEnumerator LoadLastLogin()
    {
        var DBTask = dbref.Child("users").Child(user.UserId).Child("LastLogin").GetValueAsync();
        yield return new WaitUntil(() => DBTask.IsCompleted);
        if (DBTask.Exception != null)
        {
            Debug.LogWarning($"Failed to Save task with {DBTask.Exception}");
        }
        else
        {
            DataSnapshot snapshot = DBTask.Result;
            Debug.Log("Load Complete");
            loadLastLogin = $"{snapshot.Value}";
        }
    }

    private IEnumerator LoadLastReward()
    {
        var DBTask = dbref.Child("users").Child(user.UserId).Child("RewardLogin").GetValueAsync();
        yield return new WaitUntil(() => DBTask.IsCompleted);
        if (DBTask.Exception != null)
        {
            Debug.LogWarning($"Failed to Save task with {DBTask.Exception}");
        }
        else
        {
            DataSnapshot snapshot = DBTask.Result;
            Debug.Log("Load Complete");
            loadLastReward = $"{snapshot.Value}";
            Debug.Log(loadLastLogin);
            OnInterSect();
        }
    }

    void OnInterSect()
    {
        if (loadLastLogin.Substring(0, 12).CompareTo(loadLastReward.Substring(0, 12)) < 0)
            dayRewardBtn.interactable = true;
    }

    public void ClickReward() => StartCoroutine(SaveRewardLogin());
    private IEnumerator SaveRewardLogin()
    {
        var DBTask = dbref.Child("users").Child(user.UserId).Child("RewardLogin").SetValueAsync(DateTime.Now.ToString("yyyyMMddHHmmss"));
        yield return new WaitUntil(() => DBTask.IsCompleted);
        if (DBTask.Exception != null)
        {
            Debug.LogWarning($"Failed to Save task with {DBTask.Exception}");
        }
        else
        {
        }
    }
}
