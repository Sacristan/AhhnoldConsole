using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sacristan.Ahhnold.Core;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Sacristan.Ahhnold.Runtime
{
    public abstract class Console : MonoBehaviour
    {
        const string ColorBad = "<color=red><b>";
        const string ColorVariable = "<color=brown><b>";
        const string EndFormat = "</b></color>";
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
        private bool OpenConsoleInput
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return GetKeyDown(Key.Backquote) || GetKeyDown(Key.Quote);
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKeyDown(KeyCode.Quote);
#else
                return false;
#endif
            }
        }

        private bool ControlPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return GetKey(Key.LeftCtrl);
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKey(KeyCode.LeftControl);
#else
                return false;
#endif
            }
        }

        private bool PageUp
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return GetKeyDown(Key.PageUp);
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKeyDown(KeyCode.PageUp);
#else
                return false;
#endif
            }
        }

        private bool PageDown
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return GetKeyDown(Key.PageDown);
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKeyDown(KeyCode.PageDown);
#else
                return false;
#endif
            }
        }

        private bool Plus
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return GetKeyDown(Key.NumpadPlus); //TODO GetKeyDown(Key.Plus) not found
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus);
#else
                return false;
#endif
            }
        }

        private bool Minus
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return GetKeyDown(Key.Minus) || GetKeyDown(Key.NumpadMinus);
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus);
#else
                return false;
#endif
            }
        }

#if ENABLE_INPUT_SYSTEM
        private bool GetKey(Key key) => Keyboard.current[key].isPressed;
        private bool GetKeyDown(Key key) => Keyboard.current[key].wasPressedThisFrame;
#endif

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

            guiStyle = new GUIStyle();
            guiStyle.normal.textColor = Color.white;

            logHistory = new List<string>();
        }

        void Start()
        {
            consoleController = new ConsoleController(RegistrableCommands);
            consoleController.OnLogChanged += OnLogChanged;
            consoleController.DrawIntro();

#if ENABLE_INPUT_SYSTEM
            Keyboard.current.onTextInput += HandleInputChar;
#endif
        }

        void OnDestroy()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard.current.onTextInput -= HandleInputChar;
#endif
        }

        void Update()
        {
            if (OpenConsoleInput)
            {
                isEnabled = !isEnabled;
                if (isEnabled) StartCoroutine(BlinkTypeLine());
            }

            if (isEnabled)
            {
#if !ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
                HandleInput();
#endif
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

#if !ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
        private void HandleInput()
        {
            for (int i = 0; i < Input.inputString.Length; i++)
            {
                char c = Input.inputString[i];
                HandleInputChar(c);
            }
        }
#endif
        private void HandleInputChar(char c)
        {
            if (!isEnabled) return;

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
            if (ControlPressed)
            {
                if (Plus) textSize++;
                if (Minus) textSize--;
                textSize = Mathf.Clamp(textSize, 8, 64);
                guiStyle.fontSize = (int)textSize;
            }
        }

        private void ScaleTextScrollKeyboard()
        {
            if (PageDown)
            {
                height += textSize;
            }
            if (PageUp)
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
            pixelTex.SetPixel(0, 0, Color.white);
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
            GUI.color = Color.white;
        }
    }
}