using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    public GameObject m_loginUI;
    public GameObject m_registerUI;
    public GameObject m_playUI;
    public GameObject m_settingUI;
    public GameObject m_friendUI;
    public GameObject m_main;

    public void OptionBtn()
    {
        m_main.SetActive(false);
        m_settingUI.SetActive(true);
    }

    public void CloseOptionBtn()
    {
        m_settingUI.SetActive(false);
        m_main.SetActive(true);
    }

    public void FriendBtn()
    {
        m_friendUI.SetActive(true);
        m_main.SetActive(false);
    }

    public void CloseFriendBtn()
    {
        m_friendUI.SetActive(false);
        m_main.SetActive(true);
    }

    public void GameStart()
    {
        m_main.SetActive(false);
        m_playUI.SetActive(true);
    }

    public void LoginBtn()
    {
        m_main.SetActive(false);
        m_loginUI.SetActive(true);
    }

    public void Register()
    {
        m_loginUI.SetActive(false);
        m_registerUI.SetActive(true);
    }

    public void Back()
    {
        m_registerUI.SetActive(false);
        m_main.SetActive(true);
    }

    public void LoginPanel()
    {
        m_loginUI.SetActive(true) ;
        m_registerUI.SetActive(false);
    }

    public void CloseLogin()
    {
        m_loginUI.SetActive(false);
        m_main.SetActive(true);
    }
}