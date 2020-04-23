using UnityEngine;
using Sacristan.Ahhnold.Core;

namespace Sacristan.Ahhnold.External
{
    public static class Commands
    {
        public readonly static CommandRegistration[] RegistrableCommands = new CommandRegistration[] {
            new CommandRegistration("version", VersionAction, "Outputs game version"),
            new CommandRegistration("quit", QuitAction, "Quit Game"),

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
        #endregion
    }
}