﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sacristan.Ahhnold
{
    public class Console : MonoBehaviour
    {
        public const string VERSION = "0.3.0";

        #region Util classes
        internal partial class ConsoleCommands
        {
            public readonly static CommandRegistration[] RegistrableCommands = new CommandRegistration[0];
        }

        public class CommandRegistration
        {
            public delegate void CommandHandler(string[] args);

            public string command { get; private set; }
            public CommandHandler handler { get; private set; }
            public string help { get; private set; }

            public CommandRegistration(string command, CommandHandler handler, string help)
            {
                this.command = command;
                this.handler = handler;
                this.help = help;
            }
        }

        internal class ConsoleController
        {
            private const int ScrollbackSize = 20;
            private const string ColorBad = "<color=red><b>";
            private const string FormatCommandStart = "<color=gray><i>";
            private const string FormatCommandEnd = "</i></color>";
            private const string FormatOutput = "<color=white><b>";
            private const string EndFormatBold = "</b>";
            private const string EndFormatColor = "</color>";
            private const string EndFormatColorBold = "</b></color>";

            private const string IntroASCII = @"<color=red><b>|AHHNOLD Console " + Console.VERSION + "|" + EndFormatColorBold;

            #region Event declarations
            internal delegate void LogChangedHandler(string[] log);
            internal event LogChangedHandler LogChanged;

            internal delegate void VisibilityChangedHandler(bool visible);
            internal event VisibilityChangedHandler visibilityChanged;
            #endregion

            Queue<string> scrollback = new Queue<string>(ScrollbackSize);
            List<string> commandHistory = new List<string>();
            Dictionary<string, CommandRegistration> commands = new Dictionary<string, CommandRegistration>();

            public string[] Log { get; private set; }

            const string RepeatCmdName = "!!";

            public ConsoleController()
            {
                RegisterCommand("help", HelpAction, "Print this help.");
                RegisterCommand(RepeatCmdName, RepeatCommandAction, "Repeat last command.");
                RegisterCommand("clear", ClearAction, "Clear Console");
                RegisterCommand("hide", HideAction, "Hide the console.");

                for (int i = 0; i < ConsoleCommands.RegistrableCommands.Length; i++)
                {
                    RegisterCommand(ConsoleCommands.RegistrableCommands[i]);
                }
            }

            public void DrawIntro()
            {
                AppendLogLine(IntroASCII);
            }

            public void LogOutput(string line)
            {
                AppendLogLine(string.Format("{0}{1}{2}", FormatOutput, line, EndFormatColorBold));
            }

            public void LogError(string line)
            {
                AppendLogLine(line);
                AppendLogLine(string.Format("{0}{1}{2}", ColorBad, line, EndFormatColorBold));
            }

            public void RunCommandString(string commandString)
            {
                AppendLogLine(string.Format("{0}${1}{2}", FormatCommandStart, commandString, FormatCommandEnd));

                string[] commandSplit = parseArguments(commandString);
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
                Log = scrollback.ToArray();
                if (LogChanged != null)
                {
                    LogChanged(Log);
                }
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

            private static string[] parseArguments(string commandString)
            {
                LinkedList<char> parmChars = new LinkedList<char>(commandString.ToCharArray());
                bool inQuote = false;
                var node = parmChars.First;
                while (node != null)
                {
                    var next = node.Next;
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
                if (visibilityChanged != null)
                {
                    visibilityChanged(false);
                }
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

        #endregion

        const string ColorBad = "<color=red><b>";
        const string ColorVariable = "<color=brown><b>";
        const string endFormat = "</b></color>";

        const string WordEnabled = "<color=green><b>enabled</b></color>";
        const string WordDisabled = "<color=red><b>disabled</b></color>";
        const float BackgroundTransparency = 0.9f;

        private static Console instance;

        private static bool isEnabled;
        private static GUIStyle guiStyle;
        private float textSize = 16;
        private float height = 160;

        public static string inputTxt = "";
        private bool typeLineVisible = true;

        private Vector2 preClickPos;
        private Vector2 preClickDiff;
        private bool scrolling;

        private static List<string> logHistory;
        private string tempConsoleOutput;

        private static Texture2D pixelTex;

        private ConsoleController consoleController;

        protected void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Debug.LogErrorFormat("Trying to instantiate a second instance of Console... Destroying this component!");
                Destroy(this);
            }

            // Setup the GUIStyle
            guiStyle = new GUIStyle();
            guiStyle.normal.textColor = Color.white;

            // Setup history lists
            logHistory = new List<string>();
        }

        void Start()
        {
            consoleController = new ConsoleController();
            consoleController.LogChanged += OnLogChanged;
            consoleController.DrawIntro();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKeyDown(KeyCode.Quote))
            {
                isEnabled = !isEnabled;
                if (isEnabled) StartCoroutine(BlinkTypeLine());
            }

            if (isEnabled)
            {
                HandleInput();
                ScaleText();
            }
        }
        public void OnGUI()
        {
            if (isEnabled)
            {
                GUI.depth = -99999999;

                Rect inputFieldRect = new Rect(0, height - textSize, Screen.width, textSize);

                DrawRect(new Rect(0, 0, Screen.width, height), new Color(0, 0, 0, BackgroundTransparency));

                // History
                guiStyle.alignment = TextAnchor.UpperLeft;
                int historyCount = logHistory.Count;
                for (int i = 0; i < historyCount; i++)
                {
                    GUI.Label(new Rect(0, height - textSize - historyCount * textSize + i * textSize, Screen.width, textSize), logHistory[i], guiStyle);
                }

                string typeLine = typeLineVisible ? "_" : "";

                GUI.Label(inputFieldRect, "> " + inputTxt + typeLine, guiStyle);
            }
        }

        IEnumerator BlinkTypeLine()
        {
            float t = 0;

            while (isEnabled)
            {
                t += 4 * Time.unscaledDeltaTime;

                if (t >= 1)
                {
                    t = 0;
                    typeLineVisible = !typeLineVisible;
                }

                yield return null;
            }
        }

        public void Enable()
        {
            isEnabled = true;
        }
        public void Disable()
        {
            isEnabled = false;
        }

        void ClearConsoleHistory()
        {
            logHistory.Clear();
        }

        public static void AppendLog(string log)
        {
            instance?.consoleController?.LogOutput(log);
        }

        public static void AppendError(string log)
        {
            instance?.consoleController?.LogError(log);
        }

        public static bool GetFlagFromArg1(string[] args, bool currentValue, ref bool result)
        {
            if (args.Length == 0 || string.IsNullOrEmpty(args[0]))
            {
                result = !currentValue;
            }
            else
            {
                switch (args[0])
                {
                    case "on":
                        result = true;
                        break;
                    case "off":
                        result = false;
                        break;
                    default:
                        Console.AppendError("param1 should be on/off");
                        return false;
                }
            }

            return true;
        }


        private void HandleInput()
        {
            for (int i = 0; i < Input.inputString.Length; i++)
            {
                char c = Input.inputString[i];
                // Backspace - Remove the last character
                if (c == "\b"[0])
                {
                    if (inputTxt.Length != 0)
                    {
                        inputTxt = inputTxt.Substring(0, inputTxt.Length - 1);
                    }
                }
                else if (c == "\n"[0] || c == "\r"[0]) // "\n" for Mac, "\r" for windows.
                {
                    consoleController.RunCommandString(inputTxt);
                    inputTxt = string.Empty;
                }
                else if (c != "`"[0] && c != "'"[0]) // Write text
                {
                    inputTxt += c;
                }
            }

        }

        private void ScaleText()
        {
            ScaleTextScroll();
            ScaleTextScrollKeyboard();

            float oldHeight = height;
            height = Mathf.Round(oldHeight / textSize) * textSize;

            height = Mathf.Clamp(height, textSize * 2, Screen.height);
        }

        private void ScaleTextScroll()
        {
            float scrollAmount = Input.GetAxis("Mouse ScrollWheel");

            if (Input.GetKey(KeyCode.LeftControl))
            {
                textSize += scrollAmount * 8;

                if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
                {
                    textSize = 16;
                }

                textSize = Mathf.Clamp(textSize, 8, 64);

                guiStyle.fontSize = (int)textSize;
            }
        }

        private void ScaleTextScrollKeyboard()
        {
            // Resizing with keyboard
            if (Input.GetKeyDown(KeyCode.PageDown))
            {
                height += textSize;
            }
            if (Input.GetKeyDown(KeyCode.PageUp))
            {
                height -= textSize;
            }
        }

        private void OnLogChanged(string[] newLog)
        {
            logHistory = new List<string>(newLog);
        }

        private static void MakePixelTex()
        {
            pixelTex = new Texture2D(1, 1);
            pixelTex.SetPixel(0, 0, new Color(1, 1, 1, 1));
            pixelTex.Apply();
        }
        private static void DrawRect(Rect rect, Color color)
        {
            if (!pixelTex)
            {
                MakePixelTex();
                return;
            }

            GUI.color = color;
            GUI.DrawTexture(rect, pixelTex);
            GUI.color = new Color(1, 1, 1, 1);
        }
    }
}