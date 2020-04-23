using UnityEngine;

namespace Sacristan.Ahhnold.External
{
    public static class Commands
    {
        public readonly static CommandRegistration[] RegistrableCommands = new CommandRegistration[] {
            new CommandRegistration("version", VersionAction, "Outputs game version"),
            new CommandRegistration("quit", QuitAction, "Quit Game"),
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
            //TODO fix
            // Sacristan.Ahhnold.Console.AppendLog(string.Format("version: {0}", Application.version)); //TODO fix
        }

        #endregion
    }
}