using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sacristan.Ahhnold.Core;

public class Console : Sacristan.Ahhnold.Runtime.Console
{
    public override CommandRegistration[] RegistrableCommands => new CommandRegistration[] {
            new CommandRegistration("version", VersionAction, "Outputs game version"),
            new CommandRegistration("quit", QuitAction, "Quit Game"),
            new CommandRegistration("test", TestAction, "Test Shit"),
    };

    #region Command handlers
    static void QuitAction(string[] args)
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBPLAYER
            Application.OpenURL(webplayerQuitURL);
#else
            Application.Quit();
#endif
    }

    static void VersionAction(string[] args)
    {
        ConsoleController.Log(string.Format("version: {0}", Application.version));
    }

    static void TestAction(string[] args)
    {
        GameObject.FindObjectOfType<TestConsole>().Test();
    }
    #endregion
}