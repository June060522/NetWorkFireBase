using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Random = UnityEngine.Random;
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

    [Header("ChangePassword")]
    public TMP_InputField changePasswordField;
    public TMP_InputField changeCheckPasswordField;

    [Header("DairyReward")]
    public GameObject dayRewardBtnParent;
    public TMP_Text attendance;
    private int repeat = 0;

    [Header("Friend")]
    public TMP_Text friendNameList;
    public TMP_InputField friendName;

    public TMP_Text userNameText;

    private string eventText = "";
    public string EventText => eventText;

    private string loadLastLogin = "";
    private string loadLastReward = "";
    private void Awake()
    {
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                FirebaseApp app = Firebase.FirebaseApp.DefaultInstance;
                auth = FirebaseAuth.DefaultInstance;
                dbref = FirebaseDatabase.DefaultInstance.RootReference;
            }
            else
            {
                UnityEngine.Debug.LogError(System.String.Format(
                  "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
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
            StartCoroutine(LoadRepeat());
            StartCoroutine(LoadEvent());
            StartCoroutine(ChangeEvent());
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

    private IEnumerator SaveRepeat()
    {
        var DBTask = dbref.Child("users").Child(user.UserId).Child("Repeat").SetValueAsync(repeat);
        yield return new WaitUntil(() => DBTask.IsCompleted);
        if (DBTask.Exception != null)
        {
            Debug.LogWarning($"Failed to Save task with {DBTask.Exception}");
        }
        else
        {
            attendance.text = $"{repeat}일 째 연속 출석!";
        }
    }

    private IEnumerator LoadRepeat()
    {
        var DBTask = dbref.Child("users").Child(user.UserId).Child("Repeat").GetValueAsync();
        yield return new WaitUntil(() => DBTask.IsCompleted);
        if (DBTask.Exception != null)
        {
            Debug.LogWarning($"Failed to Save task with {DBTask.Exception}");
        }
        else
        {
            DataSnapshot snapshot = DBTask.Result;
            Debug.Log("Load Complete");
            if(snapshot.Value != null)
                repeat = int.Parse($"{snapshot.Value}");
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
            loadLastReward = $"{snapshot.Value}";
            StartCoroutine(OnInterSect());
        }
    }

    bool CheckString()
    {
        long a = 0, b = 0;
        for(int i = 0; i < 13;i++)
        {
            a *= 10;
            b *= 10;
            a += loadLastLogin[i] - '0';
            b += loadLastReward[i] - '0';
        }
        return a - 2 >= b;
    }

    IEnumerator OnInterSect()
    {
        while (true)
        {
            loadLastLogin = DateTime.Now.ToString("yyyyMMddHHmmss");
            for(int i = 0; i < 28; i++)
            {
                dayRewardBtnParent.transform.GetChild(i).GetComponent<Button>().interactable = false;
            }
            if (CheckString())
            {
                repeat = 0;
                StartCoroutine(SaveRepeat());
                dayRewardBtnParent.transform.GetChild(repeat).GetComponent<Button>().interactable = true;
                attendance.gameObject.SetActive(false);
            }
            else if (loadLastLogin.Substring(0, 13).CompareTo(loadLastReward.Substring(0, 13)) > 0)
            {
                dayRewardBtnParent.transform.GetChild(repeat).GetComponent<Button>().interactable = true;
                attendance.gameObject.SetActive(false);
            }
            else
            {
                attendance.gameObject.SetActive(true);
            }
            yield return null;
        }
    }

    public void ClickReward(Button _btn) => StartCoroutine(SaveRewardLogin(_btn));
    private IEnumerator SaveRewardLogin(Button _btn)
    {
        repeat++;
        StartCoroutine(SaveRepeat());
        loadLastReward = DateTime.Now.ToString("yyyyMMddHHmmss");
        _btn.interactable = false;
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

    public void ChangePassword()
    {
        if(changeCheckPasswordField.text == changePasswordField.text)
        {
            Firebase.Auth.FirebaseUser user = auth.CurrentUser;
            string newPassword = changePasswordField.text;
            if (user != null)
            {
                user.UpdatePasswordAsync(newPassword).ContinueWith(task => {
                    if (task.IsCanceled)
                    {
                        Debug.LogError("UpdatePasswordAsync was canceled.");
                        return;
                    }
                    if (task.IsFaulted)
                    {
                        Debug.LogError("UpdatePasswordAsync encountered an error: " + task.Exception);
                        return;
                    }
                    Debug.Log("Password updated successfully.");
                });
            }
        }
    }

    IEnumerator LoadEvent()
    {
        while (true)
        {
            var DBTask = dbref.Child("Ability").GetValueAsync();
            yield return new WaitUntil(() => DBTask.IsCompleted);
            if (DBTask.Exception != null)
            {
                Debug.LogWarning($"Load task Failed with {DBTask.Exception}");
            }
            else
            {
                DataSnapshot snapshot = DBTask.Result;
                if (snapshot != null && snapshot.Value != null)
                {
                    eventText = snapshot.Value.ToString();
                }
            }
        }
    }

    IEnumerator ChangeEvent()
    {
        string eventStr = "";
        int num;
        while (true)
        {
            num = Random.Range(0, 3);
            switch (num)
            {
                case 0:
                    eventStr = "Speed";
                    break;
                case 1:
                    eventStr = "Jelly";
                    break;
                case 2:
                    eventStr = "Jump";
                    break;
            }

            var DBTask = dbref.Child("Ability").SetValueAsync(eventStr);
            yield return new WaitUntil(() => DBTask.IsCompleted);
            if (DBTask.Exception != null)
            {
                Debug.LogWarning($"Failed to Save task with {DBTask.Exception}");
            }
            else
            {
                Debug.Log("Date Saved");
            }
            yield return new WaitForSeconds(10f);
        }
    }

    public void AddBtn()
    {
        StartCoroutine(AddFriend());
    }

    IEnumerator AddFriend()
    {
        var usersList = dbref.Child("users").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            DataSnapshot snapshot = task.Result;
            for(int i = 0; i < snapshot.ChildrenCount; i++)
            {
                snapshot
            }
        });
        yield return null;
        //var DBTask = dbref.Child("users").Child(user.UserId).Child("Friend1").SetValueAsync(friendName.text));
        //yield return new WaitUntil(() => DBTask.IsCompleted);
        //if (DBTask.Exception != null)
        //{
        //    Debug.LogWarning($"Failed to Save task with {DBTask.Exception}");
        //}
        //else
        //{
        //    Debug.Log("Date Saved");
        //}
    }
}
