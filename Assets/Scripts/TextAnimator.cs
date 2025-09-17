using Sirenix.OdinInspector;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TextAnimator : MonoBehaviour
{
    private GameObject target;
    private readonly Dictionary<int, Coroutine> typewriterJobs = new();
    private Vector3 baseLocalPos;
    private bool typewriterStarted;
    private string lastContent;

    [SerializeField] private bool floating;
    [SerializeField] private bool typewriter;

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

    void Start()
    {
        target = gameObject;
        baseLocalPos = target.transform.localPosition;
        var tmp = target.GetComponent<TMP_Text>();
        var ugui = tmp ? null : target.GetComponent<Text>();
        lastContent = tmp ? tmp.text : ugui ? ugui.text : string.Empty;
    }

    void Update()
    {
        if (floating) AnimFloat();

        var tmp = target.GetComponent<TMP_Text>();
        var ugui = tmp ? null : target.GetComponent<Text>();
        var content = tmp ? tmp.text : ugui ? ugui.text : string.Empty;

        if (typewriter && !typewriterStarted)
        {
            typewriterStarted = true;
            lastContent = content;
            AnimTypewriter(target, content, charsPerSecond, useUnscaledTime, resetBefore);
        }
        else if (typewriter && typewriterStarted && content != lastContent)
        {
            lastContent = content;
            AnimTypewriter(target, content, charsPerSecond, useUnscaledTime, resetBefore);
        }
        else if (!typewriter && typewriterStarted)
        {
            typewriterStarted = false;
        }
    }

    public void AnimFloat()
    {
        target.transform.localPosition = baseLocalPos + Vector3.up * Mathf.Sin(Time.unscaledTime * speed) * amplitude;
    }

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

        typewriterJobs.Remove(label.GetInstanceID());
    }
    
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

        typewriterJobs.Remove(label.GetInstanceID());
    }
}
