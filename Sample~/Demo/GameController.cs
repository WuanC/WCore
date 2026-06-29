using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WGUI;
using WGUI.Demo;

public class GameController : MonoBehaviour
{
    void Start()
    {
        UIManager.Instance.Show<LoadingScreen>();
    }
}
