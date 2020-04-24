using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sacristan.Ahhnold.Core;

namespace Sacristan.Ahhnold.Runtime
{
    public abstract class Console : MonoBehaviour
    {
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

        public virtual CommandRegistration[] RegistrableCommands => new CommandRegistration[0];

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
            consoleController = new ConsoleController(RegistrableCommands);
            consoleController.OnLogChanged += OnLogChanged;
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
                        ConsoleController.LogError("param1 should be on/off");
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