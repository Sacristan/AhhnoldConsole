using System;
using System.Collections.Generic;
using Sacristan.Ahhnold.Core;

namespace Sacristan.Ahhnold.Core
{
    public class ConsoleController
    {
        public const string VERSION = "0.3.1";

        private const int ScrollbackSize = 20;
        private const string ColorBad = "<color=red><b>";
        private const string FormatCommandStart = "<color=gray><i>";
        private const string FormatCommandEnd = "</i></color>";
        private const string FormatOutput = "<color=white><b>";
        private const string EndFormatBold = "</b>";
        private const string EndFormatColor = "</color>";
        private const string EndFormatColorBold = "</b></color>";
        private const string IntroASCII = @"<color=red><b>|AHHNOLD Console " + VERSION + "|" + EndFormatColorBold;
        const string RepeatCmdName = "!!";

        #region Event declarations
        public delegate void LogChangedHandler(string[] log);

        public event LogChangedHandler OnLogChanged;
        internal delegate void VisibilityChangedHandler(bool visible);
        internal event VisibilityChangedHandler OnVisibilityChanged;
        #endregion
        Queue<string> scrollback = new Queue<string>(ScrollbackSize);
        List<string> commandHistory = new List<string>();
        Dictionary<string, CommandRegistration> commands = new Dictionary<string, CommandRegistration>();

        static ConsoleController instance;

        public string[] Logs { get; private set; }

        public ConsoleController(CommandRegistration[] commandRegistrations)
        {
            instance = this;

            RegisterCommand("help", HelpAction, "Print this help.");
            RegisterCommand(RepeatCmdName, RepeatCommandAction, "Repeat last command.");
            RegisterCommand("clear", ClearAction, "Clear Console");
            RegisterCommand("hide", HideAction, "Hide the console.");

            for (int i = 0; i < commandRegistrations.Length; i++)
            {
                RegisterCommand(commandRegistrations[i]);
            }
        }

        ~ConsoleController()
        {
            instance = null;
        }

        public void DrawIntro()
        {
            AppendLogLine(IntroASCII);
        }

        public static void Log(string line)
        {
            instance.AppendLogLine(string.Format("{0}{1}{2}", FormatOutput, line, EndFormatColorBold));
        }

        public static void LogError(string line)
        {
            instance.AppendLogLine(line);
            instance.AppendLogLine(string.Format("{0}{1}{2}", ColorBad, line, EndFormatColorBold));
        }

        public void RunCommandString(string commandString)
        {
            AppendLogLine(string.Format("{0}${1}{2}", FormatCommandStart, commandString, FormatCommandEnd));

            string[] commandSplit = ParseArguments(commandString);
            string[] args = new string[0];

            if (commandSplit.Length < 1)
            {
                AppendLogLine(string.Format("{0}Unable to process command '{1}'{2}", ColorBad, commandString, EndFormatColorBold));
                return;

            }
            else if (commandSplit.Length >= 2)
            {
                int numArgs = commandSplit.Length - 1;
                args = new string[numArgs];
                Array.Copy(commandSplit, 1, args, 0, numArgs);
            }
            RunCommand(commandSplit[0].ToLower(), args);
            commandHistory.Add(commandString);
        }

        #region Private

        private void AppendLogLine(string line)
        {
            if (scrollback.Count >= ScrollbackSize)
            {
                scrollback.Dequeue();
            }
            scrollback.Enqueue(line);

            UpdateCLI();
        }

        private void UpdateCLI()
        {
            Logs = scrollback.ToArray();
            OnLogChanged?.Invoke(Logs);
        }

        private void RegisterCommand(CommandRegistration commandRegistration)
        {
            commands.Add(commandRegistration.command, commandRegistration);
        }

        private void RegisterCommand(string command, CommandRegistration.CommandHandler handler, string help)
        {
            commands.Add(command, new CommandRegistration(command, handler, help));
        }

        private void RunCommand(string command, string[] args)
        {
            CommandRegistration reg = null;
            if (!commands.TryGetValue(command, out reg))
            {
                AppendLogLine(string.Format("{0}Unknown command '{1}', type 'help' for list.{2}", ColorBad, command, EndFormatColorBold));
            }
            else
            {
                if (reg.handler == null)
                {
                    AppendLogLine(string.Format("{0}Unable to process command '{1}', handler was null.{2}", ColorBad, command, EndFormatColorBold));
                }
                else
                {
                    reg.handler(args);
                }
            }
        }

        private static string[] ParseArguments(string commandString)
        {
            LinkedList<char> parmChars = new LinkedList<char>(commandString.ToCharArray());
            bool inQuote = false;

            LinkedListNode<char> node = parmChars.First;
            while (node != null)
            {
                LinkedListNode<char> next = node.Next;
                if (node.Value == '"')
                {
                    inQuote = !inQuote;
                    parmChars.Remove(node);
                }
                if (!inQuote && node.Value == ' ')
                {
                    node.Value = '\n';
                }
                node = next;
            }

            char[] parmCharsArr = new char[parmChars.Count];
            parmChars.CopyTo(parmCharsArr, 0);
            return (new string(parmCharsArr)).Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        #endregion

        #region Default Commands
        void HelpAction(string[] args)
        {
            foreach (CommandRegistration reg in commands.Values)
            {
                AppendLogLine(string.Format("{0}: {1}", reg.command, reg.help));
            }
        }

        void ClearAction(string[] args)
        {
            scrollback.Clear();
            UpdateCLI();
        }

        void HideAction(string[] args)
        {
            OnVisibilityChanged?.Invoke(false);
        }

        void RepeatCommandAction(string[] args)
        {
            for (int cmdIdx = commandHistory.Count - 1; cmdIdx >= 0; --cmdIdx)
            {
                string cmd = commandHistory[cmdIdx];
                if (String.Equals(RepeatCmdName, cmd))
                {
                    continue;
                }
                RunCommandString(cmd);
                break;
            }
        }
        #endregion
    }
}
