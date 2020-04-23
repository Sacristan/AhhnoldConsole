using UnityEngine;

namespace Sacristan.Ahhnold
{
    internal partial class ConsoleCommands
    {
        public readonly static Console.CommandRegistration[] RegistrableCommands = new Console.CommandRegistration[] {
            new Console.CommandRegistration("version", VersionAction, "Outputs game version"),
            new Console.CommandRegistration("quit", QuitAction, "Quit Game"),
        };

        #region Command handlers
        private static void QuitAction(string[] args)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBPLAYER
            Application.OpenURL(webplayerQuitURL);
#else
            Application.Quit();
#endif
        }

        private static void VersionAction(string[] args)
        {
            Console.AppendLog(string.Format("version: {0}", Application.version));
        }

        #endregion
    }
}