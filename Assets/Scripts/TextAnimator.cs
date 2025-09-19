using Sirenix.OdinInspector;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;

[DisallowMultipleComponent]
public class TextAnimator : MonoBehaviour
{
    private GameObject target;
    private readonly Dictionary<int, Coroutine> typewriterJobs = new();
    private Vector3 baseLocalPos;
    private bool typewriterDone;
    private bool typewriterStarted;
    private string lastContent = "";
    private bool inputMode;
    private string inputBuffer = "";
    private bool caretVisible;
    private Coroutine caretJob;
    public event Action<TextAnimator, string> InputSubmitted;
    private bool inputLocked;

    [SerializeField] private bool floating;
    [SerializeField] private bool typewriter;
    [SerializeField] private bool inputField;

    [Header("Floating Animation Settings")]
    [Tooltip("Speed of the floating animation")]
    [ShowIf("floating")]
    [SerializeField] private float speed = 2f;
    [Tooltip("How high the text floats up and down")]
    [ShowIf("floating")]
    [SerializeField] private float amplitude = 0.5f;

    [Header("Typewriter Animation Settings")]
    [Tooltip("Speed of the typewriter animation")]
    [ShowIf("typewriter")]
    [SerializeField] private float charsPerSecond = 30f;
    [Tooltip("If true, the typewriter animation will use unscaled time (ignoring time scale)")]
    [ShowIf("typewriter")]
    [SerializeField] private bool useUnscaledTime = true;
    [Tooltip("If true, the text will be cleared before starting the typewriter animation")]
    [ShowIf("typewriter")]
    [SerializeField] private bool resetBefore = true;

    [Header("Input Field Settings")]
    [SerializeField] private bool autoAcceptInput = true;
    [SerializeField] private List<Key> submitKeys = new() { Key.Enter, Key.NumpadEnter };
    [SerializeField] private float caretBlinkSeconds = 0.5f;

    private readonly List<char> _typedChars = new();

    void OnEnable()
    {
        target = gameObject;

        var tmp = target.GetComponent<TMP_Text>();
        var ugui = tmp ? null : target.GetComponent<Text>();

        string full = tmp ? tmp.text : ugui ? lastContent : string.Empty;
        lastContent = full;

        typewriterDone = false;

        var kb = Keyboard.current;
        if (kb != null) kb.onTextInput += OnTextInput;
        InputSystem.onDeviceChange += OnDeviceChange;

        if (floating)
        {
            target.transform.localPosition = baseLocalPos;
        }

        if (typewriter)
        {
            typewriterStarted = false;
            if (tmp)
            {
                tmp.maxVisibleCharacters = 0;
                tmp.canvasRenderer.SetAlpha(0f);
            }
            else if (ugui)
            {
                ugui.text = string.Empty;
                ugui.canvasRenderer.SetAlpha(0f);
            }
        }
        else
        {
            if (tmp) tmp.canvasRenderer.SetAlpha(1f);
            else if (ugui) ugui.canvasRenderer.SetAlpha(1f);
        }
    }

    void Awake()
    {
        target = gameObject;
        baseLocalPos = target.transform.localPosition;

        var tmp = target.GetComponent<TMP_Text>();
        var ugui = tmp ? null : target.GetComponent<Text>();

        lastContent = tmp ? tmp.text : ugui ? ugui.text : string.Empty;

        if (typewriter)
        {
            if (tmp)
            {
                tmp.ForceMeshUpdate();
                tmp.maxVisibleCharacters = 0;
                tmp.canvasRenderer.SetAlpha(0f);
            }
            else if (ugui)
            {
                ugui.text = string.Empty;
                ugui.canvasRenderer.SetAlpha(0f);
            }
        }
        else
        {
            if (tmp) tmp.canvasRenderer.SetAlpha(1f);
            else if (ugui) ugui.canvasRenderer.SetAlpha(1f);
        }
    }

    void Update()
    {
        var tmp = target.GetComponent<TMP_Text>();
        var ugui = tmp ? null : target.GetComponent<Text>();
        var content = tmp ? tmp.text : ugui ? ugui.text : string.Empty;

        int id = tmp ? tmp.GetInstanceID() : ugui ? ugui.GetInstanceID() : 0;
        bool isRunning = id != 0 && typewriterJobs.ContainsKey(id);

        if (floating) AnimFloat();
        if (typewriter && !inputMode && !typewriterStarted)
        {
            typewriterStarted = true;
            var startText = tmp ? content : ugui ? lastContent : string.Empty;
            lastContent = startText;

            if (tmp)
            {
                tmp.maxVisibleCharacters = 0;
                tmp.canvasRenderer.SetAlpha(1f);
            }
            else if (ugui)
            {
                ugui.text = string.Empty;
                ugui.canvasRenderer.SetAlpha(1f);
            }

            typewriterDone = false;

            AnimTypewriter(target, startText, charsPerSecond, useUnscaledTime, resetBefore);
        }
        else if (typewriter && !inputMode && !inputLocked && typewriterStarted && !isRunning && content != lastContent)
        {
            lastContent = content;

            typewriterDone = false;

            AnimTypewriter(target, content, charsPerSecond, useUnscaledTime, resetBefore);
        }
        else if (!typewriter && typewriterStarted)
        {
            typewriterStarted = false;
        }

        if (inputField && typewriterDone && !isRunning && autoAcceptInput && !inputMode && !inputLocked)
        {
            if (tmp) BeginInputModeTMP(tmp);
        }

        if (inputMode && target && target.TryGetComponent(out TMP_Text liveLabel))
        {
            var kb = Keyboard.current;
            bool submitted = false;

            if (_typedChars.Count > 0)
            {
                for (int i = 0; i < _typedChars.Count; i++)
                {
                    char c = _typedChars[i];
                    if (c != '\n' && c != '\r' && c != '\b')
                    {
                        inputBuffer += c;
                    }
                    else if (c == '\n' || c == '\r')
                    {
                        submitted = true;
                    }
                    else if (c == '\b')
                    {
                        if (inputBuffer.Length > 0)
                            inputBuffer = inputBuffer.Substring(0, inputBuffer.Length - 1);
                    }
                }
                _typedChars.Clear();
            }

            if (!submitted && kb != null && kb.backspaceKey.wasPressedThisFrame)
            {
                if (inputBuffer.Length > 0)
                    inputBuffer = inputBuffer.Substring(0, inputBuffer.Length - 1);
            }

            if (!submitted && kb != null)
            {
                for (int i = 0; i < submitKeys.Count; i++)
                {
                    var key = submitKeys[i];
                    if (kb[key].wasPressedThisFrame)
                    {
                        submitted = true;
                        break;
                    }
                }
            }

            if (submitted)
            {
                SubmitInput(liveLabel);
            }
            else
            {
                RedrawInputLineTMP(liveLabel);
            }
        }
    }

    void OnDisable()
    {
        var kb = Keyboard.current;
        if (kb != null) kb.onTextInput -= OnTextInput;
        InputSystem.onDeviceChange -= OnDeviceChange;

        foreach (var kvp in typewriterJobs)
        {
            if (kvp.Value != null) StopCoroutine(kvp.Value);
        }
        typewriterJobs.Clear();
        typewriterStarted = false;
        typewriterDone = false;

        if (caretJob != null) { StopCoroutine(caretJob); caretJob = null; }
        inputMode = false;
        inputBuffer = "";
        caretVisible = false;
        inputLocked = false;

        var tmp = GetComponent<TMP_Text>();
        var ugui = tmp ? null : GetComponent<Text>();

        if (tmp)
        {
            tmp.maxVisibleCharacters = 0;
            tmp.canvasRenderer.SetAlpha(0f);
        }
        else if (ugui)
        {
            ugui.text = string.Empty;
            ugui.canvasRenderer.SetAlpha(0f);
        }
    }

    /// <summary>
    /// Makes the text float up and down in a sine wave pattern.
    /// </summary>
    public void AnimFloat()
    {
        target.transform.localPosition = baseLocalPos + Vector3.up * Mathf.Sin(Time.unscaledTime * speed) * amplitude;
    }

    /// <summary>
    /// Starts a typewriter animation on the given GameObject's TMP_Text or Text component.
    /// </summary>
    public void AnimTypewriter(GameObject obj, string content, float charsPerSecond = 30f, bool useUnscaledTime = true, bool resetBefore = true)
    {
        if (!obj) return;
        if (!obj.activeSelf) obj.SetActive(true);

        if (obj.TryGetComponent(out TMP_Text tmp))
        {
            int key = tmp.GetInstanceID();
            if (typewriterJobs.TryGetValue(key, out var running)) { StopCoroutine(running); typewriterJobs.Remove(key); }
            var job = StartCoroutine(TypewriterRoutineTMP(tmp, content, charsPerSecond, useUnscaledTime, resetBefore));
            typewriterJobs[key] = job;
            return;
        }

        if (obj.TryGetComponent(out Text ugui))
        {
            int key = ugui.GetInstanceID();
            if (typewriterJobs.TryGetValue(key, out var running)) { StopCoroutine(running); typewriterJobs.Remove(key); }
            var job = StartCoroutine(TypewriterRoutineUGUI(ugui, content, charsPerSecond, useUnscaledTime, resetBefore));
            typewriterJobs[key] = job;
            return;
        }

        Debug.LogWarning("AnimTypewriter: No TMP_Text or Text on " + obj.name);
    }

    /// <summary>
    /// Typewriter animation coroutine for TMP_Text components.
    /// </summary>
    private IEnumerator TypewriterRoutineTMP(TMP_Text label, string fullText, float cps, bool unscaled, bool resetBefore)
    {
        if (!label) yield break;

        if (resetBefore) label.text = string.Empty;
        label.text = fullText;
        label.ForceMeshUpdate();
        int total = label.textInfo.characterCount;
        label.maxVisibleCharacters = 0;

        float t = 0f;
        int visible = 0;
        while (visible < total && label)
        {
            t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            int target = Mathf.Min(total, Mathf.FloorToInt(t * cps));
            if (target != visible)
            {
                visible = target;
                label.maxVisibleCharacters = visible;
            }
            yield return null;
        }
        if (label) label.maxVisibleCharacters = total;

        typewriterDone = true;

        typewriterJobs.Remove(label.GetInstanceID());
    }

    /// <summary>
    /// Typewriter animation coroutine for UGUI Text components.
    /// </summary>
    private IEnumerator TypewriterRoutineUGUI(Text label, string fullText, float cps, bool unscaled, bool resetBefore)
    {
        if (!label) yield break;

        if (resetBefore) label.text = string.Empty;

        float t = 0f;
        int shown = 0;
        int total = fullText.Length;

        while (shown < total && label)
        {
            t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            int target = Mathf.Min(total, Mathf.FloorToInt(t * cps));
            if (target != shown)
            {
                shown = target;
                label.text = fullText.Substring(0, shown);
            }
            yield return null;
        }
        if (label) label.text = fullText;

        typewriterDone = true;

        typewriterJobs.Remove(label.GetInstanceID());
    }

    /// <summary>
    /// Begins input mode for TMP_Text components.
    /// </summary>
    private void BeginInputModeTMP(TMP_Text label)
    {
        inputMode = true;
        inputBuffer = "";
        caretVisible = false;
        if (caretJob != null) StopCoroutine(caretJob);
        caretJob = StartCoroutine(CaretBlinkTMP(label));
        RedrawInputLineTMP(label);
    }

    /// <summary>
    /// Caret blinking coroutine for TMP_Text components.
    /// </summary>
    private IEnumerator CaretBlinkTMP(TMP_Text label)
    {
        while (inputMode && label)
        {
            caretVisible = !caretVisible;
            RedrawInputLineTMP(label);
            yield return new WaitForSecondsRealtime(caretBlinkSeconds);
        }
    }

    /// <summary>
    /// Redraws the input line with the current buffer and caret state for TMP_Text components
    /// </summary>
    private void RedrawInputLineTMP(TMP_Text label)
    {
        string prompt = lastContent;
        string caret = caretVisible ? "_" : "";
        label.text = prompt + " " + inputBuffer + caret;
        label.ForceMeshUpdate();
        label.maxVisibleCharacters = int.MaxValue;
    }

    /// <summary>
    /// Handles text input events from the keyboard.
    /// </summary>
    private void OnTextInput(char c)
    {
        if (!inputMode) return;

        if (c == '\n' || c == '\r')
        {
            return;
        }

        if (c == '\b')
        {
            if (inputBuffer.Length > 0)
                inputBuffer = inputBuffer.Substring(0, inputBuffer.Length - 1);
            return;
        }

        inputBuffer += c;
    }

    /// <summary>
    /// Submits the current input buffer and exits input mode for TMP_Text components.
    /// </summary>
    private void SubmitInput(TMP_Text label)
    {
        inputMode = false;
        inputLocked = true;
        if (caretJob != null) { StopCoroutine(caretJob); caretJob = null; }

        label.text = lastContent + " " + inputBuffer;
        label.ForceMeshUpdate();

        InputSubmitted?.Invoke(this, inputBuffer);
    }

    void OnApplicationFocus(bool hasFocus)
    {
        var kb = Keyboard.current;

        if (!hasFocus)
        {
            if (kb != null) kb.onTextInput -= OnTextInput;
            return;
        }

        if (kb != null) kb.onTextInput -= OnTextInput;
        if (kb != null) kb.onTextInput += OnTextInput;

        if (inputMode && target && target.TryGetComponent(out TMP_Text tmp))
        {
            if (caretJob == null) caretJob = StartCoroutine(CaretBlinkTMP(tmp));
            RedrawInputLineTMP(tmp);
        }
    }

    void OnApplicationPause(bool paused)
    {
        OnApplicationFocus(!paused);
    }

    // This will re-subscribe to keyboard input if the keyboard is reconnected after being disconnected.
    // Take note of this as it can help avoid issues with lost input on device changes.
    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (!(device is Keyboard)) return;

        var kb = Keyboard.current;
        if (kb == null) return;
        kb.onTextInput -= OnTextInput;

        if (change == InputDeviceChange.Added ||
            change == InputDeviceChange.Reconnected)
        {
            kb.onTextInput += OnTextInput;
        }
    }

}
