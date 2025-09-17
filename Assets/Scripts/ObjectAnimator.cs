using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class ObjectAnimator : MonoBehaviour
{
    public static ObjectAnimator instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    /// <summary>
    /// Fades the object out over time. Use fadeOutTime to control the speed of the fade out in seconds.
    /// </summary>
    public void AnimFadeOut(GameObject obj, float duration = 0.5f, bool deactivateOnEnd = true, bool useUnscaledTime = true)
    {
        if (!obj) return;
        var cg = obj.GetComponent<CanvasGroup>();
        if (!cg) cg = obj.AddComponent<CanvasGroup>();
        cg.interactable = false;
        cg.blocksRaycasts = false;
        if (!obj.activeSelf) obj.SetActive(true);
        StartCoroutine(FadeRoutine(cg, cg.alpha, 0f, duration, useUnscaledTime, deactivateOnEnd));
    }

    private IEnumerator FadeRoutine(CanvasGroup cg, float from, float to, float duration, bool unscaled, bool deactivateOnEnd)
    {
        if (!cg) yield break;
        if (duration <= 0f)
        {
            cg.alpha = to;
            if (deactivateOnEnd && Mathf.Approximately(to, 0f)) cg.gameObject.SetActive(false);
            yield break;
        }
        float t = 0f;
        while (t < duration && cg)
        {
            t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            cg.alpha = Mathf.Lerp(from, to, k * k * (3f - 2f * k));
            yield return null;
        }
        if (cg)
        {
            cg.alpha = to;
            if (deactivateOnEnd && Mathf.Approximately(to, 0f)) cg.gameObject.SetActive(false);
        }
    }

}
