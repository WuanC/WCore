using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WGUI;
using WGUI.Demo;

public class PlayScreen : MonoBehaviour
{
    public Button setting;

    void Start()
    {
        setting.onClick.AddListener(() =>
        {
            UIManager.Instance.Show<SettingScreen>();
        });
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            NotficationHelper.Show("This is a notification message!");
        }
    }
}
